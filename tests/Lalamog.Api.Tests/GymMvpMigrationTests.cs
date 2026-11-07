using Lalamog.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;
namespace Lalamog.Api.Tests;
public class GymMvpMigrationTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _app;
    public GymMvpMigrationTests(ApiFixture app) { _app=app; }
    [Fact]
    public async Task UpgradeRenamesWithoutLosingVenueFightAccountOrCustomGrantData()
    {
        string name="LalamogTests_GymUpgrade_"+Guid.NewGuid().ToString("N");
        var connection=new SqlConnectionStringBuilder(_app.Connection){InitialCatalog=name};
        await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            var migrator=db.GetService<IMigrator>();await migrator.MigrateAsync("20261107073133_ExplicitMapGeography");
            Guid first=Guid.NewGuid(),second=Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO AppUsers(Id,Email,NormalizedEmail,PasswordHash,AccessFailedCount) VALUES ({first},'upgrade-first@example.test','UPGRADE-FIRST@EXAMPLE.TEST','preserved-hash',0),({second},'upgrade-second@example.test','UPGRADE-SECOND@EXAMPLE.TEST','other-hash',0);
                INSERT INTO Roles(Name,Description) VALUES ('User','Edited role description');
                INSERT INTO SystemFeatures(Code,Description) VALUES ('custom_preserved','Edited custom permission');
                INSERT INTO UserRoles(UserId,RoleId) SELECT {first},Id FROM Roles WHERE Name='User';
                INSERT INTO RoleSystemFeatures(RoleId,SystemFeatureId) SELECT r.Id,f.Id FROM Roles r CROSS JOIN SystemFeatures f WHERE r.Name='User';
                INSERT INTO Regions(Code,Name) VALUES ('TESTREGION','Edited region');
                INSERT INTO Cities(Code,Name,RegionId) SELECT 'TESTCITY','Edited city',Id FROM Regions;
                INSERT INTO Maps(Slug,Name,Address,Latitude,Longitude,CityId,IsDemo) SELECT 'imported-gym','Edited venue','Edited address',14.5,121.1,Id,0 FROM Cities;
                INSERT INTO Matches(User1Id,User2Id,CreatedAt) VALUES ({first},{second},SYSDATETIMEOFFSET());
                INSERT INTO Fights(MatchId,MapId,ScheduledAt,Location,Address,Status,CreatedAt,UpdatedAt) SELECT ma.Id,g.Id,SYSDATETIMEOFFSET(),'','','completed',SYSDATETIMEOFFSET(),SYSDATETIMEOFFSET() FROM Matches ma CROSS JOIN Maps g;
                INSERT INTO FightConfirmations(FightId,UserId,EmergencyContactName,EmergencyContactPhone,AcceptedAt) SELECT Id,{first},'Fictional preserved contact','000',SYSDATETIMEOFFSET() FROM Fights;");
            int gymId=await db.Database.SqlQueryRaw<int>("SELECT CAST(Id AS int) AS [Value] FROM Maps").SingleAsync();
            int roleId=await db.Database.SqlQueryRaw<int>("SELECT Id AS [Value] FROM Roles WHERE Name='User'").SingleAsync();
            await migrator.MigrateAsync();
            var gym=await db.Gyms.SingleAsync();Assert.Equal(gymId,gym.Id);Assert.Equal("Edited venue",gym.Name);Assert.Equal("Edited address",gym.Address);Assert.Equal(14.5,gym.Latitude);Assert.Null(gym.OwnerUserId);Assert.False(gym.IsPublished);
            var fight=await db.Fights.SingleAsync();Assert.Equal(gym.Id,fight.GymId);Assert.Equal("completed",fight.Status);
            Assert.False((await db.FightConfirmations.SingleAsync()).AllowPublicIdentity);
            Assert.Equal("preserved-hash",(await db.Users.FindAsync(first))!.PasswordHash);
            var role=await db.Roles.SingleAsync();Assert.Equal(roleId,role.Id);Assert.Equal("FIGHTER",role.Name);Assert.Equal("Edited role description",role.Description);Assert.Single(await db.RoleSystemFeatures.ToListAsync());
            await DatabaseSeeder.SeedAsync(db);await DatabaseSeeder.SeedAsync(db);
            gym=await db.Gyms.SingleAsync(g=>g.Slug=="imported-gym");Assert.Null(gym.OwnerUserId);Assert.False(gym.IsPublished);Assert.Equal("Edited venue",gym.Name);
            Assert.True(await db.RoleSystemFeatures.AnyAsync(g=>g.RoleId==roleId&&g.SystemFeature.Code=="custom_preserved"));
            Assert.False(db.Database.HasPendingModelChanges());
        }
        finally { if(!name.StartsWith("LalamogTests_GymUpgrade_",StringComparison.Ordinal))throw new InvalidOperationException("Unexpected database.");await db.Database.EnsureDeletedAsync(); }
    }
}
