using System.Net;
using System.Net.Http.Json;
using Lalamog.Api.Data;
using Lalamog.Api.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lalamog.Api.Tests;

public class DevelopmentStartupTests
{
    [Fact]
    public async Task DevelopmentCreatesMissingDatabaseAndSeeds_ThenRestartPreservesProfiles()
    {
        string server = Environment.GetEnvironmentVariable("LALAMOG_TEST_SQL")
            ?? "Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
        string database = "LalamogTests_Startup_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder(server) { InitialCatalog = database };
        string directory = Path.Combine(Path.GetTempPath(), database);
        Guid originalId;
        try
        {
            // The database does not exist; no test initializer creates or migrates it.
            using (var app = new StartupFactory(connection.ConnectionString, directory))
            using (var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false }))
            {
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
                originalId = await LoginAsync(client);
                ProfileResponse profile = (await client.GetFromJsonAsync<ProfileResponse>("/api/profiles/me"))!;
                Assert.Equal("Seed-Alex", profile.FighterName);
                var edit = new ProfileRequest(profile.Nickname, "Keep this profile edit across restart", profile.FightingStyle, profile.ExperienceLevel, profile.Location, profile.Availability);
                Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/profiles/me", edit)).StatusCode);
                using var scope = app.Services.CreateScope();
                AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.Equal(6, await db.Users.CountAsync());
                Assert.Equal(6, await db.Profiles.CountAsync());
                Assert.Equal(5, await db.Matches.CountAsync());
                Assert.Equal(6, await db.Fights.CountAsync());
            }
            using (var restarted = new StartupFactory(connection.ConnectionString, directory))
            using (var client = restarted.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false }))
            {
                Assert.Equal(originalId, await LoginAsync(client));
                Assert.Equal("Keep this profile edit across restart", (await client.GetFromJsonAsync<ProfileResponse>("/api/profiles/me"))!.Bio);
                using var scope = restarted.Services.CreateScope();
                AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.Equal(6, await db.Users.CountAsync());
                Assert.Equal(6, await db.Fights.CountAsync());
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
        }
    }
}
