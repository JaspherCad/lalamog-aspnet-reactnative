using System.Net;
using System.Net.Http.Json;
using Lalamog.Api.Data;
using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lalamog.Api.Tests;

public class RolePermissionMigrationTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _app;

    public RolePermissionMigrationTests(ApiFixture app)
    {
        _app = app;
    }

    [Fact]
    public async Task PopulatedUpgradePreservesAccounts_AndStaticSeederBackfillsUserAccessOnce()
    {
        string database = "LalamogTests_RoleUpgrade_" + Guid.NewGuid().ToString("N");
        var builder = new SqlConnectionStringBuilder(_app.Connection) { InitialCatalog = "master" };
        await using var master = new SqlConnection(builder.ConnectionString);
        await master.OpenAsync();
        await using (var create = new SqlCommand($"CREATE DATABASE [{database}]", master))
        {
            await create.ExecuteNonQueryAsync();
        }
        try
        {
            builder.InitialCatalog = database;
            Guid userId = Guid.NewGuid();
            string email = userId.ToString("N") + "@example.test";
            string hash = PasswordHashing.Hash("Practice123!Example");
            await using (var baseline = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(builder.ConnectionString).Options))
            {
                await baseline.GetService<IMigrator>().MigrateAsync("20261102074948_PhilippineMapLocations");
                await baseline.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO [AppUsers] ([Id],[Email],[NormalizedEmail],[PasswordHash],[AccessFailedCount])
                    VALUES ({userId},{email},{email.ToUpperInvariant()},{hash},0);
                    INSERT INTO [Profiles] ([UserId],[Nickname],[FighterName],[NormalizedFighterName],[Bio],[FightingStyle],[ExperienceLevel],[Location],[Availability])
                    VALUES ({userId},'Upgrade alias','Role-Upgrade','ROLE-UPGRADE','Preserved role-upgrade bio','Boxing',1,'Manila','Weekends');");
            }
            using WebApplicationFactory<AppDbContext> upgraded = _app.WithWebHostBuilder(ConfigureUpgrade);
            void ConfigureUpgrade(IWebHostBuilder host)
            {
                host.UseSetting("ConnectionStrings:DefaultConnection", builder.ConnectionString);
            }
            using (var scope = upgraded.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.MigrateAsync();
                await DatabaseSeeder.SeedAsync(db);
                var original = await db.Users.FindAsync(userId);
                Assert.Equal(hash, original!.PasswordHash);
                Assert.Equal("Preserved role-upgrade bio", (await db.Profiles.FindAsync(userId))!.Bio);
                Assert.Equal(8, await db.SystemFeatures.CountAsync());
                Assert.Equal(2, await db.Roles.CountAsync());
                Assert.Single(await (from link in db.UserRoles where link.UserId == userId select link).ToListAsync());
                Assert.True(await (from link in db.UserRoles where link.UserId == userId && link.Role.Name == "User" select link).AnyAsync());
                Assert.False(await (from link in db.UserRoles where link.UserId == userId && link.Role.Name == "Admin" select link).AnyAsync());
                Assert.False(db.Database.HasPendingModelChanges());
                var assignment = await (from stored in db.UserRoles where stored.UserId == userId select stored).SingleAsync();
                // Composite uniqueness and foreign keys are enforced by SQL Server.
                var duplicate = await Assert.ThrowsAsync<SqlException>(DuplicateAssignment);
                Assert.Contains(duplicate.Number, new[] { 2601, 2627 });
                Task DuplicateAssignment()
                {
                    return db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [UserRoles] ([UserId],[RoleId]) VALUES ({userId},{assignment.RoleId})");
                }
                db.UserRoles.Remove(assignment);
                await db.SaveChangesAsync();
                await DatabaseSeeder.SeedAsync(db);
                await DatabaseSeeder.SeedAsync(db);
                Assert.False(await (from stored in db.UserRoles where stored.UserId == userId select stored).AnyAsync());
            }
            using HttpClient client = upgraded.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
            var csrf = (await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf"))!;
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.Token);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Practice123!Example"))).StatusCode);
            Assert.Equal(userId, (await client.GetFromJsonAsync<AuthResponse>("/api/auth/me"))!.UserId);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/profiles/me")).StatusCode);
        }
        finally
        {
            if (!database.StartsWith("LalamogTests_RoleUpgrade_", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Unexpected role-upgrade test database.");
            }
            SqlConnection.ClearAllPools();
            await using var drop = new SqlCommand($"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]", master);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
