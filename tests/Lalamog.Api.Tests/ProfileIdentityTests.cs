using System.Net;
using System.Net.Http.Json;
using Lalamog.Api.Data;
using Lalamog.Api.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Lalamog.Api.Tests;

public class ProfileIdentityTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _app;

    public ProfileIdentityTests(ApiFixture app)
    {
        _app = app;
    }

    private async Task<HttpClient> AccountAsync()
    {
        HttpClient client = _app.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        await CsrfAsync(client);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(Guid.NewGuid().ToString("N") + "@example.test", "Practice123!Example", "Crow"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await CsrfAsync(client);
        return client;
    }

    private static async Task CsrfAsync(HttpClient client)
    {
        Dictionary<string, string>? response = await client.GetFromJsonAsync<Dictionary<string, string>>("/api/auth/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", response!["token"]);
    }

    [Fact]
    public async Task NamesAreUniqueIgnoringCase_AliasesCanRepeat_AndLegacyUpdatesRetainNames()
    {
        using HttpClient first = await AccountAsync();
        using HttpClient second = await AccountAsync();
        string name = "Miko-" + Guid.NewGuid().ToString("N");
        var request = new ProfileRequest("Crow", "Bio", "kickboxing", 2, "Rizal", "Weekends") { FighterName = name };
        Assert.Equal(HttpStatusCode.OK, (await first.PutAsJsonAsync("/api/profiles/me", request)).StatusCode);
        request.FighterName = name.ToUpperInvariant();
        HttpResponseMessage duplicate = await second.PutAsJsonAsync("/api/profiles/me", request);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Contains("already taken", await duplicate.Content.ReadAsStringAsync());

        using HttpClient newcomer = _app.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        await CsrfAsync(newcomer);
        var duplicateRegistration = new RegisterRequest(Guid.NewGuid().ToString("N") + "@example.test", "Practice123!Example", "Crow") { FighterName = name.ToUpperInvariant() };
        Assert.Equal(HttpStatusCode.Conflict, (await newcomer.PostAsJsonAsync("/api/auth/register", duplicateRegistration)).StatusCode);
        duplicateRegistration.FighterName = "New-" + Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.OK, (await newcomer.PostAsJsonAsync("/api/auth/register", duplicateRegistration)).StatusCode);
        await CsrfAsync(newcomer);
        ProfileResponse? newcomerProfile = await newcomer.GetFromJsonAsync<ProfileResponse>("/api/profiles/me");
        Assert.Equal(duplicateRegistration.FighterName, newcomerProfile!.FighterName);
        Assert.Equal("Crow", newcomerProfile.Nickname);

        request.FighterName = "Jae-" + Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.OK, (await second.PutAsJsonAsync("/api/profiles/me", request)).StatusCode);
        ProfileResponse? a = await first.GetFromJsonAsync<ProfileResponse>("/api/profiles/me");
        ProfileResponse? b = await second.GetFromJsonAsync<ProfileResponse>("/api/profiles/me");
        Assert.Equal("Crow", a!.Nickname);
        Assert.Equal("Crow", b!.Nickname);
        Assert.NotEqual(a.FighterName, b.FighterName);

        var legacy = new ProfileRequest("Grim Reaper", "Edited", "wrestling", 2, "Rizal", "Weekends");
        Assert.Equal(HttpStatusCode.OK, (await first.PutAsJsonAsync("/api/profiles/me", legacy)).StatusCode);
        ProfileResponse? reloaded = await first.GetFromJsonAsync<ProfileResponse>("/api/profiles/me");
        Assert.Equal(name, reloaded!.FighterName);
        Assert.Equal("Grim Reaper", reloaded.Nickname);

        string contested = "Shared-" + Guid.NewGuid().ToString("N");
        var one = new ProfileRequest("Crow", "", "kickboxing", 1, "", "") { FighterName = contested };
        var two = new ProfileRequest("Crow", "", "kickboxing", 1, "", "") { FighterName = contested.ToUpperInvariant() };
        HttpResponseMessage[] attempts = await Task.WhenAll(first.PutAsJsonAsync("/api/profiles/me", one), second.PutAsJsonAsync("/api/profiles/me", two));
        int successes = 0;
        int conflicts = 0;
        foreach (HttpResponseMessage attempt in attempts)
        {
            if (attempt.StatusCode == HttpStatusCode.OK) successes++;
            if (attempt.StatusCode == HttpStatusCode.Conflict) conflicts++;
        }
        Assert.Equal(1, successes);
        Assert.Equal(1, conflicts);

        one.FighterName = "Invalid name with spaces";
        Assert.Equal(HttpStatusCode.BadRequest, (await first.PutAsJsonAsync("/api/profiles/me", one)).StatusCode);
    }

    [Fact]
    public async Task UpgradePreservesDuplicateAliasesAndProfileData()
    {
        string database = "LalamogTests_NameUpgrade_" + Guid.NewGuid().ToString("N");
        var builder = new SqlConnectionStringBuilder(_app.Connection) { InitialCatalog = "master" };
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using (SqlCommand create = connection.CreateCommand())
        {
            create.CommandText = $"CREATE DATABASE [{database}]";
            await create.ExecuteNonQueryAsync();
        }
        try
        {
            builder.InitialCatalog = database;
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(builder.ConnectionString).Options;
            await using var db = new AppDbContext(options);
            IMigrator migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261027231736_InitialWebMvp");
            Guid a = Guid.NewGuid();
            Guid b = Guid.NewGuid();
            await db.Database.ExecuteSqlRawAsync(@"
                INSERT INTO [AspNetUsers] ([Id],[EmailConfirmed],[PhoneNumberConfirmed],[TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount])
                VALUES ({0},0,0,0,0,0),({1},0,0,0,0,0);
                INSERT INTO [Profiles] ([UserId],[Nickname],[Bio],[FightingStyle],[ExperienceLevel],[Location],[Availability])
                VALUES ({0},'Crow','Preserved bio','wrestling',2,'Rizal','Weekends'),
                       ({1},'Crow','Preserved bio','wrestling',2,'Rizal','Weekends');", a, b);
            await migrator.MigrateAsync();
            var profiles = await db.Profiles.AsNoTracking().ToListAsync();
            Assert.Equal(2, profiles.Count);
            foreach (var profile in profiles)
            {
                Assert.Equal("Crow", profile.Nickname);
                Assert.Equal("Preserved bio", profile.Bio);
                Assert.Equal("wrestling", profile.FightingStyle);
                Assert.Equal("Rizal", profile.Location);
                Assert.Equal("Weekends", profile.Availability);
                Assert.Equal("fighter-" + profile.UserId.ToString("N"), profile.FighterName);
                Assert.Equal(profile.FighterName.ToUpperInvariant(), profile.NormalizedFighterName);
            }
            Assert.NotEqual(profiles[0].FighterName, profiles[1].FighterName);
        }
        finally
        {
            if (!database.StartsWith("LalamogTests_NameUpgrade_", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected test database.");
            SqlConnection.ClearAllPools();
            await using SqlCommand drop = connection.CreateCommand();
            drop.CommandText = $"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]";
            await drop.ExecuteNonQueryAsync();
        }
    }
}
