using Lalamog.Api.Data;
using Lalamog.Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Lalamog.Api.Tests;

public class PublicMapMigrationTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _app;
    public PublicMapMigrationTests(ApiFixture app) { _app = app; }

    [Fact]
    public async Task ExplicitGeographyPreservesOldPinsAndIds_AndEnforcesNamedForeignKeys()
    {
        string database = "LalamogTests_ExplicitMaps_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder(_app.Connection) { InitialCatalog = database };
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString).Options);
        var migrator = db.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync("20261107063728_PublicGymMaps");
            await db.Database.ExecuteSqlRawAsync(@"
                INSERT INTO MapAreas(Code,Name,Level,ParentCode) VALUES ('R','Edited region','region',NULL);
                INSERT INTO MapAreas(Code,Name,Level,ParentCode) VALUES ('P','Edited province','province','R');
                INSERT INTO MapAreas(Code,Name,Level,ParentCode) VALUES ('C','Provincial city','city','P'),('I','Independent city','city','R');
                INSERT INTO MapAreas(Code,Name,Level,ParentCode) VALUES ('S','Internal subcity','submunicipality','I');
                INSERT INTO MapAreas(Code,Name,Level,ParentCode) VALUES ('B','Barangay','barangay','S');
                INSERT INTO Maps(Slug,Name,Address,Latitude,Longitude,AreaCode,IsDemo) VALUES
                    ('preserved-city','Edited city gym','Edited address',14.5,121.1,'C',1),
                    ('preserved-barangay','Barangay gym','Other address',14.6,121.2,'B',0);");
            int originalId = await db.Database.SqlQueryRaw<int>("SELECT CAST(Id AS int) AS [Value] FROM Maps WHERE Slug='preserved-city'").SingleAsync();
            await migrator.MigrateAsync();
            var provincial = await db.Gyms.Include("City.Province.Region").SingleAsync(m => m.Slug == "preserved-city");
            Assert.Equal(originalId, provincial.Id); Assert.Equal("Edited address", provincial.Address);
            Assert.Equal("Edited region", provincial.City.Province!.Region.Name); Assert.Null(provincial.City.RegionId);
            var independent = await db.Gyms.Include("City.Region").Include("Barangay").SingleAsync(m => m.Slug == "preserved-barangay");
            Assert.Null(independent.City.ProvinceId); Assert.NotNull(independent.City.RegionId);
            Assert.Equal(independent.CityId, independent.Barangay!.CityId);
            var mismatch = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Gyms SET CityId={provincial.CityId} WHERE Id={independent.Id}"));
            Assert.Equal(547, mismatch.Number);
            var invalidParent = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Cities SET RegionId=NULL WHERE Id={independent.CityId}"));
            Assert.Equal(547, invalidParent.Number);
            Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name='MapAreas'").SingleAsync());
            await migrator.MigrateAsync("20261107063728_PublicGymMaps");
            Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM Maps WHERE Slug='preserved-barangay' AND AreaCode='B'").SingleAsync());
            await migrator.MigrateAsync(); db.ChangeTracker.Clear();
            Assert.Equal(2, await db.Gyms.CountAsync());
        }
        finally
        {
            if (!database.StartsWith("LalamogTests_ExplicitMaps_", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected database.");
            await db.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task PopulatedUpgradePreservesPrivateVenues_ConstraintsPreventDuplication_AndRollbackKeepsPublicSnapshots()
    {
        string database = "LalamogTests_PublicMaps_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder(_app.Connection) { InitialCatalog = database };
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString).Options);
        var migrator = db.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync("20261106053901_ServerDerivedAvatars");
            Guid a = Guid.NewGuid(), b = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO AppUsers (Id,Email,NormalizedEmail,AccessFailedCount) VALUES ({a},'first@example.test','FIRST@EXAMPLE.TEST',0),({b},'second@example.test','SECOND@EXAMPLE.TEST',0);
                INSERT INTO Matches (User1Id,User2Id,CreatedAt) VALUES ({a},{b},SYSDATETIMEOFFSET());
                INSERT INTO Fights (MatchId,ScheduledAt,Location,Address,Status,CreatedAt,UpdatedAt,Latitude,Longitude)
                SELECT Id,SYSDATETIMEOFFSET(),'Preserved venue','Preserved address','completed',SYSDATETIMEOFFSET(),SYSDATETIMEOFFSET(),14.5,121.1 FROM Matches;");
            await migrator.MigrateAsync();
            var original = await db.Fights.SingleAsync();
            Assert.Null(original.GymId); Assert.Equal("Preserved venue", original.Location); Assert.Equal(14.5, original.Latitude);
            var region = new MapRegion { Code = "0400000000", Name = "CALABARZON" };
            var city = new MapCity { Code = "0405802000", Name = "City of Antipolo", Region = region };
            db.Cities.Add(city); await db.SaveChangesAsync();
            var map = new Gym { Slug = "upgrade-test", Name = "Public upgrade venue", Address = "Public upgrade address", CityId = city.Id, Latitude = 14.6, Longitude = 121.2 };
            db.Gyms.Add(map); await db.SaveChangesAsync();
            var fight = new Fight { MatchId = original.MatchId, GymId = map.Id, Status = "completed" };
            db.Fights.Add(fight); await db.SaveChangesAsync();
            var duplicate = await Assert.ThrowsAsync<SqlException>(DuplicateVenueAsync);
            Assert.Equal(547, duplicate.Number);
            var restrictedDelete = await Assert.ThrowsAsync<SqlException>(DeleteVenueAsync);
            Assert.Equal(547, restrictedDelete.Number);
            Assert.False(db.Database.HasPendingModelChanges());
            await migrator.MigrateAsync("20261106053901_ServerDerivedAvatars");
            Assert.Equal(2, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM Fights").SingleAsync());
            Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM Fights WHERE Location='Public upgrade venue' AND Address='Public upgrade address' AND Latitude=14.6 AND Longitude=121.2").SingleAsync());
            await migrator.MigrateAsync(); db.ChangeTracker.Clear();
            Assert.Equal("Preserved address", (await db.Fights.FindAsync(original.Id))!.Address);
            Assert.Equal("Public upgrade address", (await db.Fights.FindAsync(fight.Id))!.Address);

            Task DuplicateVenueAsync() { return db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Fights SET Address='Duplicated venue' WHERE Id={fight.Id}"); }
            Task DeleteVenueAsync() { return db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Gyms WHERE Id={map.Id}"); }
        }
        finally
        {
            if (!database.StartsWith("LalamogTests_PublicMaps_", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected test database.");
            await db.Database.EnsureDeletedAsync();
        }
    }
}
