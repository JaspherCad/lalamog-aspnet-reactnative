using Lalamog.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Lalamog.Api.Tests;

public class PermissionContainerMigrationTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _app;
    public PermissionContainerMigrationTests(ApiFixture app) { _app = app; }

    [Fact]
    public async Task RoleRenamePreservesIdsAssignmentsAndRevokedGrants()
    {
        string name = "LalamogTests_PermissionUpgrade_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder(_app.Connection) { InitialCatalog = name };
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261107080243_GymBookingMvp");
            Guid userId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO AppUsers(Id,Email,NormalizedEmail,PasswordHash,AccessFailedCount) VALUES ({userId},'permission-upgrade@example.test','PERMISSION-UPGRADE@EXAMPLE.TEST','preserved-hash',0);
                INSERT INTO Roles(Name,Description) VALUES ('Fighter','Edited fighter'),('Admin','Edited admin'),('GymOwner','Edited owner');
                INSERT INTO SystemFeatures(Code,Description) VALUES ('user_canMatch','Matching'),('admin_canManageAccess','Access'),('owner_canManageGyms','Gym management');
                INSERT INTO UserRoles(UserId,RoleId) SELECT {userId},Id FROM Roles;
                INSERT INTO RoleSystemFeatures(RoleId,SystemFeatureId) SELECT r.Id,f.Id FROM Roles r CROSS JOIN SystemFeatures f WHERE r.Name='GymOwner' AND f.Code='owner_canManageGyms';");
            var before = await (from role in db.Roles.AsNoTracking() orderby role.Id select role).ToListAsync();
            await migrator.MigrateAsync();
            var after = await (from role in db.Roles.AsNoTracking() orderby role.Id select role).ToListAsync();
            Assert.Equal((from role in before select role.Id), (from role in after select role.Id));
            Assert.Equal(new[] { "FIGHTER", "ADMIN", "GYM_OWNER" }, (from role in after select role.Name));
            Assert.Equal((from role in before select role.Description), (from role in after select role.Description));
            Assert.Equal(3, await (from link in db.UserRoles where link.UserId == userId select link).CountAsync());
            Assert.Single(await db.RoleSystemFeatures.ToListAsync());
            Assert.Equal("preserved-hash", (await db.Users.FindAsync(userId))!.PasswordHash);
            await DatabaseSeeder.SeedAsync(db);
            await DatabaseSeeder.SeedAsync(db);
            // Existing empty default containers do not silently regain revoked capabilities.
            Assert.False(await (from grant in db.RoleSystemFeatures where grant.Role.Name == "FIGHTER" && grant.SystemFeature.Code == "user_canMatch" select grant).AnyAsync());
            Assert.False(await (from grant in db.RoleSystemFeatures where grant.Role.Name == "ADMIN" && grant.SystemFeature.Code == "admin_canManageAccess" select grant).AnyAsync());
            Assert.True(await (from grant in db.RoleSystemFeatures where grant.Role.Name == "GYM_OWNER" && grant.SystemFeature.Code == "owner_canManageGyms" select grant).AnyAsync());
            Assert.Equal(3, await (from link in db.UserRoles where link.UserId == userId select link).CountAsync());
            Assert.False(await (from link in db.UserRoles where link.Role.Name == "VISITOR" select link).AnyAsync());
        }
        finally
        {
            if (!name.StartsWith("LalamogTests_PermissionUpgrade_", StringComparison.Ordinal)) { throw new InvalidOperationException("Unexpected database."); }
            await db.Database.EnsureDeletedAsync();
        }
    }
}
