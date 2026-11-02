using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lalamog.Api.Data;
using Lalamog.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lalamog.Api.Tests;

public class MapFeatureTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _app;
    public MapFeatureTests(ApiFixture app) { _app = app; }

    private async Task<(HttpClient Client, Guid Id)> AccountAsync()
    {
        HttpClient client = _app.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        await CsrfAsync(client);
        var request = new RegisterRequest(Guid.NewGuid().ToString("N") + "@example.test", "Practice123!Example", "Map tester");
        HttpResponseMessage result = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Guid id = (await result.Content.ReadFromJsonAsync<AuthResponse>())!.UserId;
        await CsrfAsync(client);
        return (client, id);
    }
    private static async Task CsrfAsync(HttpClient client)
    {
        CsrfResponse? response = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", response!.Token);
    }
    private static MapLocation QcLocation()
    {
        return new MapLocation { RegionCode = "1300000000", CityCode = "1381300000", BarangayCode = "1381300082", Latitude = 14.64, Longitude = 121.04 };
    }

    [Fact]
    public async Task LocalCatalogIncludesNirNcrManilaAndIndependentCities()
    {
        using HttpClient client = _app.CreateClient();
        var regions = await client.GetFromJsonAsync<List<AreaOption>>("/api/areas/regions");
        Assert.Equal(18, regions!.Count);
        Assert.Contains(regions, area => area.Code == "1800000000");
        Assert.Empty((await client.GetFromJsonAsync<List<AreaOption>>("/api/areas/provinces?regionCode=1300000000"))!);
        var ncr = await client.GetFromJsonAsync<List<AreaOption>>("/api/areas/cities?regionCode=1300000000&provinceCode=none");
        Assert.Contains(ncr!, area => area.Code == "1381300000");
        Assert.Equal(17, ncr!.Count);
        var manila = await client.GetFromJsonAsync<List<AreaOption>>("/api/areas/barangays?cityCode=1380600000");
        Assert.True(manila!.Count > 800);
        var provinces = await client.GetFromJsonAsync<List<AreaOption>>("/api/areas/provinces?regionCode=1400000000");
        string benguet = provinces!.Single(area => area.Name == "Benguet").Code;
        var provincial = await client.GetFromJsonAsync<List<AreaOption>>("/api/areas/cities?regionCode=1400000000&provinceCode=" + benguet);
        var independent = await client.GetFromJsonAsync<List<AreaOption>>("/api/areas/cities?regionCode=1400000000&provinceCode=none");
        Assert.DoesNotContain(provincial!, area => area.Code == "1430300000");
        Assert.Contains(independent!, area => area.Code == "1430300000");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/areas/cities?regionCode=1300000000&provinceCode=" + benguet)).StatusCode);
    }

    [Fact]
    public async Task ProfilePinsPersistPrivately_AndLegacyUpdatesDoNotEraseThem()
    {
        var account = await AccountAsync(); using HttpClient owner = account.Client;
        var other = await AccountAsync(); using HttpClient observer = other.Client;
        var request = new ProfileRequest("Map tester", "Bio", "kickboxing", 2, "Old text", "Weekends") { MapLocation = QcLocation() };
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync("/api/profiles/me", request)).StatusCode);
        ProfileResponse? own = await owner.GetFromJsonAsync<ProfileResponse>("/api/profiles/me");
        Assert.Equal("Quezon City", own!.Location);
        Assert.Equal("1381300082", own.MapLocation!.BarangayCode);
        Assert.Equal(14.64, own.MapLocation.Latitude);
        Assert.Equal("2026-Q3", own.MapLocation.PsgcVersion);
        JsonElement publicProfiles = await observer.GetFromJsonAsync<JsonElement>("/api/profiles/discover");
        JsonElement publicProfile = publicProfiles.EnumerateArray().Single(profile => profile.GetProperty("userId").GetGuid() == account.Id);
        Assert.False(publicProfile.TryGetProperty("mapLocation", out _));
        Assert.False(publicProfile.TryGetProperty("latitude", out _));

        request.MapLocation = null;
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync("/api/profiles/me", request)).StatusCode);
        Assert.Equal(14.64, (await owner.GetFromJsonAsync<ProfileResponse>("/api/profiles/me"))!.MapLocation!.Latitude);
        request.MapLocation = new MapLocation();
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync("/api/profiles/me", request)).StatusCode);
        Assert.Null((await owner.GetFromJsonAsync<ProfileResponse>("/api/profiles/me"))!.MapLocation!.Latitude);
    }

    [Theory]
    [InlineData("half-coordinate")]
    [InlineData("latitude-range")]
    [InlineData("longitude-range")]
    [InlineData("wrong-city")]
    [InlineData("wrong-province")]
    [InlineData("stale-version")]
    public async Task InvalidLocationDetailsAreRejectedBeforeSaving(string problem)
    {
        var account = await AccountAsync(); using HttpClient client = account.Client;
        MapLocation location = QcLocation();
        switch (problem)
        {
            case "half-coordinate": location.Longitude = null; break;
            case "latitude-range": location.Latitude = -91; break;
            case "longitude-range": location.Longitude = 181; break;
            case "wrong-city": location.CityCode = "1380600000"; break;
            case "wrong-province": location.ProvinceCode = "1401100000"; break;
            case "stale-version": location.PsgcVersion = "2000-Q1"; break;
        }
        var request = new ProfileRequest("Map tester", "", "kickboxing", 1, "City", "") { MapLocation = location };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/profiles/me", request)).StatusCode);
        using IServiceScope scope = _app.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Null((await db.Profiles.FindAsync(account.Id))!.Latitude);
    }

    [Fact]
    public async Task VenuePinAndBarangayPersist_AndOnlyParticipantsCanReadThem()
    {
        var first = await AccountAsync(); using HttpClient a = first.Client;
        var second = await AccountAsync(); using HttpClient b = second.Client;
        var third = await AccountAsync(); using HttpClient outsider = third.Client;
        await a.PostAsJsonAsync("/api/matches/swipes", new SwipeRequest(second.Id, "like"));
        var result = await b.PostAsJsonAsync("/api/matches/swipes", new SwipeRequest(first.Id, "like"));
        long matchId = (await result.Content.ReadFromJsonAsync<SwipeResponse>())!.MatchId!.Value;
        JsonElement matches = await a.GetFromJsonAsync<JsonElement>("/api/matches");
        Assert.False(matches[0].GetProperty("opponent").TryGetProperty("mapLocation", out _));
        var request = new ScheduleRequest(matchId, DateTimeOffset.UtcNow.AddDays(3), "Manual venue", "An editable manual address") { MapLocation = QcLocation() };
        HttpResponseMessage response = await a.PostAsJsonAsync("/api/fights", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        FightResponse? fight = await response.Content.ReadFromJsonAsync<FightResponse>();
        FightResponse? reloaded = await b.GetFromJsonAsync<FightResponse>("/api/fights/" + fight!.Id);
        Assert.Equal(121.04, reloaded!.MapLocation!.Longitude);
        Assert.Equal("1381300082", reloaded.MapLocation.BarangayCode);
        Assert.Equal("An editable manual address", reloaded.Address);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync("/api/fights/" + fight.Id)).StatusCode);
    }
}
