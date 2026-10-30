using System.Net;
using System.Net.Http.Json;
using Lalamog.Api.DTOs;
using Lalamog.Api.Data;
using Lalamog.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using Xunit;
namespace Lalamog.Api.Tests;

public class CoreFlowTests(ApiFixture app) : IClassFixture<ApiFixture>
{
    private const string Password = "Practice123!Example";
    private async Task<(HttpClient Client, Guid Id, string Email)> Account()
    {
        var client = app.CreateClient(new() { HandleCookies = true, AllowAutoRedirect = false });
        await Csrf(client);
        var email = Guid.NewGuid().ToString("N") + "@example.test";
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, Password, "Fighter"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var account = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        await Csrf(client);
        return (client, account.UserId, email);
    }
    private static async Task Csrf(HttpClient client)
    {
        var data = await client.GetFromJsonAsync<Dictionary<string, string>>("/api/auth/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", data!["token"]);
    }
    private static async Task<SwipeResponse> Swipe(HttpClient client, Guid target, string direction)
    {
        var response = await client.PostAsJsonAsync("/api/matches/swipes", new SwipeRequest(target, direction));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SwipeResponse>())!;
    }
    private async Task<(HttpClient A, Guid AId, HttpClient B, Guid BId, long MatchId)> Pair()
    {
        var a = await Account(); var b = await Account();
        Assert.False((await Swipe(a.Client, b.Id, "like")).Matched);
        var result = await Swipe(b.Client, a.Id, "like");
        Assert.True(result.Matched);
        return (a.Client, a.Id, b.Client, b.Id, result.MatchId!.Value);
    }

    [Fact]
    public async Task Accounts_RequireCsrf_RestoreSession_AndLogout()
    {
        var anonymous = app.CreateClient(new() { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest("nobody@example.test", Password))).StatusCode);
        var a = await Account();
        Assert.Equal(a.Id, (await a.Client.GetFromJsonAsync<AuthResponse>("/api/auth/me"))!.UserId);
        a.Client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await a.Client.PostAsJsonAsync("/api/auth/logout", new { })).StatusCode);
        await Csrf(a.Client);
        Assert.Equal(HttpStatusCode.NoContent, (await a.Client.PostAsJsonAsync("/api/auth/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await a.Client.GetAsync("/api/auth/me")).StatusCode);
        await Csrf(a.Client);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await a.Client.PostAsJsonAsync("/api/auth/login", new LoginRequest(a.Email, "wrong"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await a.Client.PostAsJsonAsync("/api/auth/login", new LoginRequest(a.Email, Password))).StatusCode);
    }

    [Fact]
    public async Task Matching_HandlesChoiceChanges_Repeats_AndDiscovery()
    {
        var a = await Account(); var b = await Account();
        Assert.False((await Swipe(a.Client, b.Id, "pass")).Matched);
        Assert.False((await Swipe(b.Client, a.Id, "like")).Matched);
        var match = await Swipe(a.Client, b.Id, "like");
        Assert.True(match.Matched);
        Assert.Equal(match.MatchId, (await Swipe(a.Client, b.Id, "like")).MatchId);
        Assert.Equal(match.MatchId, (await Swipe(a.Client, b.Id, "pass")).MatchId);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await a.Client.PostAsJsonAsync("/api/matches/swipes", new SwipeRequest(a.Id, "like"))).StatusCode);
        var discover = (await a.Client.GetFromJsonAsync<List<ProfileResponse>>("/api/profiles/discover"))!;
        Assert.DoesNotContain(discover, p => p.UserId == a.Id || p.UserId == b.Id);
        Assert.Single((await a.Client.GetFromJsonAsync<List<MatchResponse>>("/api/matches"))!);
    }

    [Fact]
    public async Task SimultaneousReciprocalLikes_ProduceExactlyOneMatch()
    {
        for (var i = 0; i < 4; i++)
        {
            var a = await Account(); var b = await Account();
            await Task.WhenAll(Swipe(a.Client, b.Id, "like"), Swipe(b.Client, a.Id, "like"));
            var first = (await a.Client.GetFromJsonAsync<List<MatchResponse>>("/api/matches"))!;
            var second = (await b.Client.GetFromJsonAsync<List<MatchResponse>>("/api/matches"))!;
            Assert.Single(first); Assert.Single(second); Assert.Equal(first[0].Id, second[0].Id);
        }
    }

    [Fact]
    public async Task Chat_IsPrivate_RejectsWhitespace_AndSupportsRecovery()
    {
        var pair = await Pair(); var outsider = await Account();
        var path = $"/api/matches/{pair.MatchId}/messages";
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.Client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await outsider.Client.PostAsJsonAsync(path, new MessageRequest("intrusion"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await pair.A.PostAsJsonAsync(path, new MessageRequest("  "))).StatusCode);
        var sent = await pair.A.PostAsJsonAsync(path, new MessageRequest("Hello"));
        var message = (await sent.Content.ReadFromJsonAsync<ChatMessage>())!;
        Assert.Equal(pair.AId, message.SenderId);
        Assert.Single((await pair.B.GetFromJsonAsync<List<ChatMessage>>(path))!);
        Assert.Empty((await pair.B.GetFromJsonAsync<List<ChatMessage>>(path + "?after=" + message.Id))!);
        Assert.Single((await pair.B.GetFromJsonAsync<List<ChatMessage>>(path + "?before=" + (message.Id + 1)))!);
    }

    [Fact]
    public async Task Scheduling_PreservesWaivers_AuthorizesParticipants_AndEnforcesTransitions()
    {
        var pair = await Pair(); var outsider = await Account();
        var at = app.Clock.Now.AddHours(4);
        var response = await pair.A.PostAsJsonAsync("/api/fights", new ScheduleRequest(pair.MatchId, at, "Gym", "Manila"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fight = (await response.Content.ReadFromJsonAsync<FightResponse>())!;
        var path = $"/api/fights/{fight.Id}";
        Assert.Equal(HttpStatusCode.Conflict,
            (await pair.B.PostAsJsonAsync("/api/fights", new ScheduleRequest(pair.MatchId, at, "Other", "Manila"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.Client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await outsider.Client.PostAsJsonAsync(path + "/confirm", new ConfirmRequest(true, "Name", "123"))).StatusCode);
        Assert.Empty((await outsider.Client.GetFromJsonAsync<List<FightResponse>>("/api/fights"))!);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await pair.A.PostAsJsonAsync(path + "/confirm", new ConfirmRequest(false, "Name", "123"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await pair.A.PostAsJsonAsync(path + "/confirm", new ConfirmRequest(true, "", ""))).StatusCode);
        var confirmations = await Task.WhenAll(
            pair.A.PostAsJsonAsync(path + "/confirm", new ConfirmRequest(true, "Contact A", "123")),
            pair.B.PostAsJsonAsync(path + "/confirm", new ConfirmRequest(true, "Contact B", "456")));
        Assert.All(confirmations, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        var confirmed = (await pair.A.GetFromJsonAsync<FightResponse>(path))!;
        Assert.Equal("confirmed", confirmed.Status); Assert.Equal(2, confirmed.Confirmations.Count);
        Assert.Equal(HttpStatusCode.Conflict, (await pair.A.PostAsJsonAsync(path + "/complete", new { })).StatusCode);
        app.Clock.Now = at.AddMinutes(1);
        Assert.Equal(HttpStatusCode.OK, (await pair.B.PostAsJsonAsync(path + "/complete", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await pair.A.PostAsJsonAsync(path + "/cancel", new CancelRequest("late"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await pair.A.PostAsJsonAsync("/api/fights", new ScheduleRequest(pair.MatchId, app.Clock.Now.AddDays(1), "Gym", "Manila"))).StatusCode);
    }

    [Fact]
    public async Task CompetingSchedules_AllowOne_AndCancellationReleasesTheSlot()
    {
        var pair = await Pair();
        var request = new ScheduleRequest(pair.MatchId, app.Clock.Now.AddDays(2), "Gym", "Manila");
        var responses = await Task.WhenAll(pair.A.PostAsJsonAsync("/api/fights", request), pair.B.PostAsJsonAsync("/api/fights", request));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        var fight = (await responses.Single(r => r.IsSuccessStatusCode).Content.ReadFromJsonAsync<FightResponse>())!;
        Assert.Equal(HttpStatusCode.OK,
            (await pair.B.PostAsJsonAsync($"/api/fights/{fight.Id}/cancel", new CancelRequest("Reschedule"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await pair.A.PostAsJsonAsync("/api/fights", request)).StatusCode);
    }

    [Fact]
    public async Task Photos_ValidateDecodedContent_SizeAndPath_AndRemainReadableAcrossStorageInstances()
    {
        var a = await Account();
        using var bitmap = new SKBitmap(2, 2);
        bitmap.Erase(SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        foreach (var (format, extension) in new[] { (SKEncodedImageFormat.Png, "png"), (SKEncodedImageFormat.Jpeg, "jpg"), (SKEncodedImageFormat.Webp, "webp") })
        {
            using var encoded = image.Encode(format, 90);
            using var body = new MultipartFormDataContent { { new ByteArrayContent(encoded.ToArray()), "file", "photo." + extension } };
            var uploaded = await a.Client.PostAsync("/api/profiles/me/avatar", body);
            Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
            var profile = (await uploaded.Content.ReadFromJsonAsync<ProfileResponse>())!;
            Assert.Equal(HttpStatusCode.OK, (await app.CreateClient().GetAsync(profile.AvatarUrl)).StatusCode);
        }
        foreach (var bytes in new[] { new byte[20], new byte[5 * 1024 * 1024 + 1] })
        {
            using var invalid = new MultipartFormDataContent { { new ByteArrayContent(bytes), "file", "fake.png" } };
            Assert.Equal(HttpStatusCode.BadRequest, (await a.Client.PostAsync("/api/profiles/me/avatar", invalid)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await a.Client.GetAsync("/api/media/..%5cappsettings.json")).StatusCode);
    }

    [Fact]
    public async Task DatabaseContainsNoUserDefinedTriggers()
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.triggers WHERE is_ms_shipped = 0").SingleAsync();
        Assert.Equal(0, count);
    }
}
