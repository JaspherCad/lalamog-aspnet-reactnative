using System.Net;
using System.Net.Http.Json;
using Lalamog.Api.Data;
using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Repositories;
using Lalamog.Api.Services;
using Microsoft.AspNetCore.Http;
using SkiaSharp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lalamog.Api.Tests;

public class NativeUserMigrationTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _app;

    public NativeUserMigrationTests(ApiFixture app)
    {
        _app = app;
    }

    [Fact]
    public async Task UpgradePreservesAccountsPasswordsProfilesAndMetadata_AndRollbackKeepsNewAccounts()
    {
        const string previousMigration = "20261102030056_UniqueFighterNames";
        const string password = "Practice123!Example";
        using var storageScope = _app.Services.CreateScope();
        var storage = storageScope.ServiceProvider.GetRequiredService<IFileStorage>();
        using var bitmap = new SKBitmap(2, 2);
        bitmap.Erase(SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 90);
        byte[] portrait = encoded.ToArray();
        string previousAvatarBlobName = await storage.SavePhotoAsync(
            new FormFile(new MemoryStream(portrait), 0, portrait.Length, "file", "old.png"), CancellationToken.None);
        const string previousHash = "AQAAAAIAAYagAAAAEAABAgMEBQYHCAkKCwwNDg/x+D7MHTUzdF2GX4CN2BrHMo1FJG0yVF0JTgSoDn74Iw==";
        string database = "LalamogTests_UserUpgrade_" + Guid.NewGuid().ToString("N");
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
            string connectionString = builder.ConnectionString;
            await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options);
            IMigrator migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync(previousMigration);
            Guid originalId = Guid.NewGuid();
            string email = originalId.ToString("N") + "@example.test";
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO [AspNetUsers]
                    ([Id],[Email],[NormalizedEmail],[UserName],[NormalizedUserName],[PasswordHash],
                     [AccessFailedCount],[EmailConfirmed],[PhoneNumberConfirmed],[TwoFactorEnabled],[LockoutEnabled])
                VALUES ({originalId},{email},{email.ToUpperInvariant()},{email},{email.ToUpperInvariant()},{previousHash},2,1,0,0,1);
                INSERT INTO [Profiles]
                    ([UserId],[Nickname],[FighterName],[NormalizedFighterName],[Bio],[FightingStyle],[ExperienceLevel],[Location],[Availability],[AvatarKey])
                VALUES ({originalId},'Crow','Native-Migration','NATIVE-MIGRATION','Preserved bio','Boxing',1,'Rizal','Weekends',{previousAvatarBlobName});
                INSERT INTO [AspNetUserClaims] ([UserId],[ClaimType],[ClaimValue]) VALUES ({originalId},'migration-test','kept');");

            var profiles = new ProfileService(new ProfileRepository(db), storage, storageScope.ServiceProvider.GetRequiredService<IAreaService>());
            await profiles.MigrateLegacyAvatarsAsync(CancellationToken.None);
            await migrator.MigrateAsync();
            var stored = await db.Users.AsNoTracking().SingleAsync();
            Assert.Equal(originalId, stored.Id);
            Assert.Equal(previousHash, stored.PasswordHash);
            Assert.Equal(2, stored.AccessFailedCount);
            Assert.True(PasswordHashing.Verify(stored.PasswordHash, password));
            Assert.NotEmpty(stored.Version);
            var upgradedProfile = await db.Profiles.AsNoTracking().SingleAsync();
            Assert.Equal("Preserved bio", upgradedProfile.Bio);
            Assert.Equal("Rizal", upgradedProfile.Location);
            var migratedPhoto = await storage.OpenAsync($"avatars/{originalId:N}/current", CancellationToken.None);
            await migratedPhoto.Content.DisposeAsync();
            Assert.Null(upgradedProfile.Latitude);
            Assert.Null(upgradedProfile.BarangayCode);

            await using (var check = new SqlConnection(connectionString))
            {
                await check.OpenAsync();
                await using var command = new SqlCommand("SELECT COUNT(*) FROM [RetiredAuthUserClaims] WHERE [ClaimType] = 'migration-test' AND [ClaimValue] = 'kept'", check);
                Assert.Equal(1, Convert.ToInt32(await command.ExecuteScalarAsync()));
            }

            Guid newAccountId;
            using (WebApplicationFactory<AppDbContext> upgraded = _app.WithWebHostBuilder(host => host.UseSetting("ConnectionStrings:DefaultConnection", connectionString)))
            {
                using (var scope = upgraded.Services.CreateScope())
                {
                    var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await appDb.Database.MigrateAsync();
                    await DatabaseSeeder.SeedAsync(appDb);
                }
                using HttpClient client = upgraded.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
                await PrepareCsrfAsync(client);
                HttpResponseMessage login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email.ToUpperInvariant(), password));
                Assert.Equal(HttpStatusCode.OK, login.StatusCode);
                Assert.Equal(originalId, (await client.GetFromJsonAsync<AuthResponse>("/api/auth/me"))!.UserId);
                Assert.Equal("Crow", (await client.GetFromJsonAsync<ProfileResponse>("/api/profiles/me"))!.Nickname);
                await PrepareCsrfAsync(client);
                string newEmail = Guid.NewGuid().ToString("N") + "@example.test";
                HttpResponseMessage registered = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(newEmail, password, "New alias"));
                Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
                newAccountId = (await registered.Content.ReadFromJsonAsync<AuthResponse>())!.UserId;
            }

            await migrator.MigrateAsync(previousMigration);
            await using (var check = new SqlConnection(connectionString))
            {
                await check.OpenAsync();
                await using var command = new SqlCommand("SELECT COUNT(*) FROM [AspNetUsers] WHERE [Id] = @id", check);
                command.Parameters.AddWithValue("@id", newAccountId);
                Assert.Equal(1, Convert.ToInt32(await command.ExecuteScalarAsync()));
            }
            await migrator.MigrateAsync();
            db.ChangeTracker.Clear();
            var retainedPhoto = await storage.OpenAsync($"avatars/{originalId:N}/current", CancellationToken.None);
            await retainedPhoto.Content.DisposeAsync();
            Assert.NotNull(await db.Users.FindAsync(newAccountId));
            Assert.NotNull(await db.Profiles.FindAsync(newAccountId));
            Assert.False(db.Database.HasPendingModelChanges());
        }
        finally
        {
            if (!database.StartsWith("LalamogTests_UserUpgrade_", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Unexpected migration test database.");
            }
            SqlConnection.ClearAllPools();
            await using var drop = new SqlCommand($"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]", master);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task PrepareCsrfAsync(HttpClient client)
    {
        CsrfResponse? csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.Token);
    }
}
