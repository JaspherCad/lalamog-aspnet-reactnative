using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lalamog.Api.Contracts;
using Lalamog.Api.Data;
using Lalamog.Api.Models;
using Lalamog.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lalamog.Api.Tests;

public class WorkflowTests
{
    private static async Task<(HttpClient Client, AuthResponse Auth)> Register(ApiFactory factory, string nickname)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"{Guid.NewGuid():N}@example.com", "Training123!", nickname));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, auth);
    }

    [Fact]
    public async Task Accounts_profiles_matching_and_private_chat_work_end_to_end()
    {
        using var factory = new ApiFactory();
        var (alice, a) = await Register(factory, "Alice");
        var (bob, b) = await Register(factory, "Bob");
        var (outsider, _) = await Register(factory, "Outsider");
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/profiles/me")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await alice.PostAsJsonAsync("/api/matches/swipes", new SwipeRequest(a.UserId, "like"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await alice.PostAsJsonAsync("/api/matches/swipes", new SwipeRequest(b.UserId, "wrong"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await alice.PostAsJsonAsync("/api/matches/swipes", new SwipeRequest(Guid.NewGuid(), "like"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await alice.PutAsJsonAsync("/api/profiles/me",
            new ProfileRequest("Alice", "Learning", "Boxing", 2, "Manila", "Weekends"))).StatusCode);
        Assert.Equal("Manila", (await alice.GetFromJsonAsync<ProfileResponse>("/api/profiles/me"))!.Location);

        await alice.PostAsJsonAsync("/api/matches/swipes", new SwipeRequest(b.UserId, "pass"));
        var discover = await alice.GetFromJsonAsync<List<ProfileResponse>>("/api/profiles/discover");
        Assert.DoesNotContain(discover!, p => p.UserId == b.UserId || p.UserId == a.UserId);
        Assert.Empty((await alice.GetFromJsonAsync<List<MatchResponse>>("/api/matches"))!);
        await bob.PostAsJsonAsync("/api/matches/swipes", new SwipeRequest(a.UserId, "like"));
        var result = await alice.PostAsJsonAsync("/api/matches/swipes", new SwipeRequest(b.UserId, "like"));
        var match = (await result.Content.ReadFromJsonAsync<SwipeResponse>())!;
        Assert.True(match.Matched);
        for (var i = 0; i < 3; i++) await alice.PostAsJsonAsync("/api/matches/swipes", new SwipeRequest(b.UserId, "like"));
        Assert.Single((await alice.GetFromJsonAsync<List<MatchResponse>>("/api/matches"))!);
        var path = $"/api/matches/{match.MatchId}/messages";
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsJsonAsync(path, new MessageRequest("Intrusion"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await alice.PostAsJsonAsync(path, new MessageRequest("   "))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await alice.PostAsJsonAsync(path, new MessageRequest("Meet at the gym?"))).StatusCode);
        var messages = await bob.GetFromJsonAsync<List<ChatMessage>>(path);
        Assert.Equal("Meet at the gym?", Assert.Single(messages!).Content);
        Assert.Equal(a.UserId, messages![0].SenderId);
        Assert.Empty((await bob.GetFromJsonAsync<List<ChatMessage>>($"{path}?before={messages[0].Id}"))!);
    }

    [Fact]
    public async Task Scheduling_requires_membership_both_confirmations_and_results_apply_once()
    {
        using var factory = new ApiFactory();
        var (alice, a) = await Register(factory, "Alice");
        var (bob, b) = await Register(factory, "Bob");
        var (outsider, _) = await Register(factory, "Outsider");
        await alice.PostAsJsonAsync("/api/matches/swipes", new SwipeRequest(b.UserId, "like"));
        var mutual = await bob.PostAsJsonAsync("/api/matches/swipes", new SwipeRequest(a.UserId, "like"));
        var matchId = (await mutual.Content.ReadFromJsonAsync<SwipeResponse>())!.MatchId!.Value;
        var request = new ScheduleRequest(matchId, DateTimeOffset.UtcNow.AddDays(1), "Manila gym");
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsJsonAsync("/api/fights", request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await alice.PostAsJsonAsync("/api/fights", request with { ScheduledAt = DateTimeOffset.UtcNow.AddDays(-1) })).StatusCode);
        var response = await alice.PostAsJsonAsync("/api/fights", request);
        var fight = (await response.Content.ReadFromJsonAsync<FightResponse>())!;
        var path = $"/api/fights/{fight.Id}";
        Assert.Equal(HttpStatusCode.Conflict, (await bob.PostAsJsonAsync("/api/fights", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsJsonAsync($"{path}/confirm", new ConfirmRequest(true))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await alice.PostAsJsonAsync($"{path}/confirm", new ConfirmRequest(false))).StatusCode);
        var first = await alice.PostAsJsonAsync($"{path}/confirm", new ConfirmRequest(true));
        Assert.Equal("scheduled", (await first.Content.ReadFromJsonAsync<FightResponse>())!.Status);
        var second = await bob.PostAsJsonAsync($"{path}/confirm", new ConfirmRequest(true));
        Assert.Equal("confirmed", (await second.Content.ReadFromJsonAsync<FightResponse>())!.Status);
        var outcome = new ResultRequest("win", a.UserId, "Good rounds");
        Assert.Equal(HttpStatusCode.Conflict, (await alice.PostAsJsonAsync($"{path}/result", outcome)).StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Fights.FindAsync(fight.Id))!.ScheduledAt = DateTimeOffset.UtcNow.AddHours(-1);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await alice.PostAsJsonAsync($"{path}/result", outcome with { WinnerId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await alice.PostAsJsonAsync($"{path}/result", outcome with { Result = "draw" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await alice.PostAsJsonAsync($"{path}/result", outcome)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await bob.PostAsJsonAsync($"{path}/result", outcome)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await alice.PostAsJsonAsync($"{path}/cancel", new CancelRequest("Changed mind"))).StatusCode);
        Assert.Equal(1216, (await alice.GetFromJsonAsync<ProfileResponse>("/api/profiles/me"))!.Rating);
        Assert.Equal(1184, (await bob.GetFromJsonAsync<ProfileResponse>("/api/profiles/me"))!.Rating);
        var stats = await alice.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/profiles/me/stats");
        Assert.Equal(1, stats.GetProperty("wins").GetInt32());
        var next = await alice.PostAsJsonAsync("/api/fights", request);
        var nextId = (await next.Content.ReadFromJsonAsync<FightResponse>())!.Id;
        Assert.Equal(HttpStatusCode.OK, (await bob.PostAsJsonAsync($"/api/fights/{nextId}/cancel", new CancelRequest("Unavailable"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await alice.PostAsJsonAsync($"/api/fights/{nextId}/confirm", new ConfirmRequest(true))).StatusCode);
    }

    [Fact]
    public async Task Invalid_credentials_and_weak_passwords_are_rejected()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("test@example.com", "weak", "Test"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("unknown@example.com", "Training123!"))).StatusCode);
        var registered = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("test@example.com", "Training123!", "Test"));
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("TEST@example.com", "Training123!", "Duplicate"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test@example.com", "Training123!"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test@example.com", "Wrong123!"))).StatusCode);
    }

    [Theory]
    [InlineData(1200, 1200, 1, 16)]
    [InlineData(1200, 1200, 0, -16)]
    [InlineData(1200, 1200, 0.5, 0)]
    public void Elo_changes_are_balanced(int rating, int opponent, double score, int expected) =>
        Assert.Equal(expected, FightService.RatingChange(rating, opponent, score));

    [Fact]
    public void Upload_validation_rejects_html_and_accepts_supported_magic_bytes()
    {
        Assert.Null(BlobService.DetectType("<script>alert(1)</script>"u8.ToArray()));
        Assert.Null(BlobService.DetectType([]));
        Assert.Equal("image/png", BlobService.DetectType([137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0])!.Value.Mime);
        Assert.Equal("image/gif", BlobService.DetectType("GIF89a1234567"u8.ToArray())!.Value.Mime);
    }
}
