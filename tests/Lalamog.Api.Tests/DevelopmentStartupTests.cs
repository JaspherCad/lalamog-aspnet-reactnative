using System.Net;
using System.Net.Http.Json;
using Lalamog.Api.Data;
using Lalamog.Api.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Lalamog.Api.Models;
using SkiaSharp;
using Xunit;

namespace Lalamog.Api.Tests;

public class DevelopmentStartupTests
{
    [Fact]
    public async Task DevelopmentTransfersExistingAvatarBeforeMigration_AndRetainsItAfterRestart()
    {
        string server = Environment.GetEnvironmentVariable("LALAMOG_TEST_SQL")
            ?? "Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
        string database = "LalamogTests_Startup_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder(server) { InitialCatalog = database };
        string directory = Path.Combine(Path.GetTempPath(), database);
        string uploads = Path.Combine(directory, "uploads");
        Guid userId = Guid.NewGuid();
        try
        {
            Directory.CreateDirectory(uploads);
            using var bitmap = new SKBitmap(2, 2);
            bitmap.Erase(SKColors.Purple);
            using var image = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 90);
            byte[] original = encoded.ToArray();
            string legacy = Guid.NewGuid().ToString("N") + ".png";
            await File.WriteAllBytesAsync(Path.Combine(uploads, legacy), original);
            await using (var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString).Options))
            {
                await db.GetService<IMigrator>().MigrateAsync("20261104085551_AvatarBlobNames");
                db.Users.Add(new AppUser { Id = userId, Email = "retained@example.test", NormalizedEmail = "RETAINED@EXAMPLE.TEST" });
                await db.SaveChangesAsync();
                db.Profiles.Add(new FighterProfile { UserId = userId, Nickname = "Retained" });
                await db.SaveChangesAsync();
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Profiles SET AvatarBlobName = {legacy} WHERE UserId = {userId}");
            }
            for (int run = 0; run < 2; run++)
            {
                using var app = new StartupFactory(connection.ConnectionString, directory);
                using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });
                Assert.Equal(original, await client.GetByteArrayAsync($"/api/media/avatars/{userId:D}"));
                Assert.Equal(original, await File.ReadAllBytesAsync(Path.Combine(uploads, legacy)));
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID('Profiles') AND name IN ('AvatarKey','AvatarBlobName')").SingleAsync());
            }
        }
        finally
        {
            if (!database.StartsWith("LalamogTests_Startup_", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected startup database.");
            await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString).Options);
            await db.Database.EnsureDeletedAsync();
            string resolved = Path.GetFullPath(directory);
            string temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!resolved.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(resolved) != database) throw new InvalidOperationException("Unexpected startup folder.");
            if (Directory.Exists(resolved)) Directory.Delete(resolved, recursive: true);
        }
    }

    [Fact]
    public async Task DevelopmentCreatesMissingDatabaseAndSeeds_ThenRestartPreservesProfiles()
    {
        string server = Environment.GetEnvironmentVariable("LALAMOG_TEST_SQL")
            ?? "Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
        string database = "LalamogTests_Startup_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder(server) { InitialCatalog = database };
        string directory = Path.Combine(Path.GetTempPath(), database);
        Guid originalId;
        Guid likedId;
        Guid passedId;
        try
        {
            // The database does not exist; no test initializer creates or migrates it.
            using (var app = new StartupFactory(connection.ConnectionString, directory))
            using (var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false }))
            {
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
                originalId = await LoginAsync(client);
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/access/system-features")).StatusCode);
                ProfileResponse profile = (await client.GetFromJsonAsync<ProfileResponse>("/api/profiles/me"))!;
                Assert.Equal("Seed-Alex", profile.FighterName);
                var edit = new ProfileRequest(profile.Nickname, "Keep this profile edit across restart", profile.FightingStyle, profile.ExperienceLevel, profile.Location, profile.Availability);
                Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/profiles/me", edit)).StatusCode);
                using var scope = app.Services.CreateScope();
                AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.Equal(21, await db.Users.CountAsync());
                Assert.Equal(21, await db.Profiles.CountAsync());
                Assert.Equal(5, await db.Matches.CountAsync());
                Assert.Equal(6, await db.Fights.CountAsync());
                Assert.Equal(11, await db.Swipes.CountAsync());
                Assert.Equal(10, await db.Messages.CountAsync());
                Assert.Equal(9, await db.FightConfirmations.CountAsync());
                Assert.Equal(2, await db.Roles.CountAsync());
                Assert.Equal(8, await db.SystemFeatures.CountAsync());
                Assert.Equal(22, await db.UserRoles.CountAsync());
                Assert.Equal(8, await db.RoleSystemFeatures.CountAsync());

                var discovery = (await client.GetFromJsonAsync<List<ProfileResponse>>("/api/profiles/discover"))!;
                Assert.Equal(16, discovery.Count);
                likedId = (from candidate in discovery where candidate.FighterName == "Seed-Nico" select candidate.UserId).Single();
                passedId = (from candidate in discovery where candidate.FighterName == "Seed-Bea" select candidate.UserId).Single();
                foreach (var choice in new[] { (Target: likedId, Direction: "like"), (Target: passedId, Direction: "pass") })
                {
                    var response = await client.PostAsJsonAsync("/api/matches/swipes", new SwipeRequest(choice.Target, choice.Direction));
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    var swipe = (await response.Content.ReadFromJsonAsync<SwipeResponse>())!;
                    Assert.False(swipe.Matched);
                    Assert.Null(swipe.MatchId);
                }
                Assert.Equal(14, (await client.GetFromJsonAsync<List<ProfileResponse>>("/api/profiles/discover"))!.Count);
            }
            using (var restarted = new StartupFactory(connection.ConnectionString, directory))
            using (var client = restarted.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false }))
            {
                Assert.Equal(originalId, await LoginAsync(client));
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/access/system-features")).StatusCode);
                Assert.Equal("Keep this profile edit across restart", (await client.GetFromJsonAsync<ProfileResponse>("/api/profiles/me"))!.Bio);
                using var scope = restarted.Services.CreateScope();
                AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.Equal(21, await db.Users.CountAsync());
                Assert.Equal(6, await db.Fights.CountAsync());
                Assert.Equal(13, await db.Swipes.CountAsync());
                Assert.Equal(5, await db.Matches.CountAsync());
                var discovery = (await client.GetFromJsonAsync<List<ProfileResponse>>("/api/profiles/discover"))!;
                Assert.Equal(14, discovery.Count);
                Assert.DoesNotContain(likedId, from candidate in discovery select candidate.UserId);
                Assert.DoesNotContain(passedId, from candidate in discovery select candidate.UserId);
            }
        }
        finally
        {
            if (!database.StartsWith("LalamogTests_Startup_", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected startup test database.");
            SqlConnection.ClearAllPools();
            await using var master = new SqlConnection(new SqlConnectionStringBuilder(server) { InitialCatalog = "master" }.ConnectionString);
            await master.OpenAsync();
            await using var drop = new SqlCommand($"IF DB_ID(N'{database}') IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END", master);
            await drop.ExecuteNonQueryAsync();
            string resolved = Path.GetFullPath(directory);
            string temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!resolved.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(resolved) != database) throw new InvalidOperationException("Unexpected startup test folder.");
            if (Directory.Exists(resolved)) Directory.Delete(resolved, recursive: true);
        }
    }

    private static async Task<Guid> LoginAsync(HttpClient client)
    {
        await CsrfAsync(client);
        HttpResponseMessage login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("alex.seed@example.test", "Lalamog123!Test"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        AuthResponse identity = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;
        await CsrfAsync(client);
        Assert.Equal(identity.UserId, (await client.GetFromJsonAsync<AuthResponse>("/api/auth/me"))!.UserId);
        return identity.UserId;
    }

    private static async Task CsrfAsync(HttpClient client)
    {
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.Token);
    }

    private sealed class StartupFactory(string connection, string directory) : WebApplicationFactory<AppDbContext>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:DefaultConnection", connection);
            builder.UseSetting("Storage:Path", Path.Combine(directory, "uploads"));
            builder.UseSetting("Storage:Provider", "Local");
        }
    }
}
