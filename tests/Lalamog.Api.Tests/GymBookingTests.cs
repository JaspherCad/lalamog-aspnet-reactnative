using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lalamog.Api.Data;
using Lalamog.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Lalamog.Api.Tests;

public class GymBookingTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _app;
    public GymBookingTests(ApiFixture app) { _app = app; }
    private HttpClient Client() { return _app.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true }); }
    private static async Task CsrfAsync(HttpClient client)
    {
        var token = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token!.Token);
    }
    private async Task<(HttpClient Client, AuthResponse Session)> AccountAsync(string accountType = "fighter")
    {
        var client = Client(); await CsrfAsync(client);
        var result = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(Guid.NewGuid().ToString("N")+"@example.test", "Practice123!Example", "Gym tester") { AccountType = accountType });
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var session = (await result.Content.ReadFromJsonAsync<AuthResponse>())!; await CsrfAsync(client); return (client, session);
    }
    private async Task<(HttpClient A, HttpClient B, long MatchId)> PairAsync()
    {
        var a = await AccountAsync(); var b = await AccountAsync();
        await a.Client.PostAsJsonAsync("/api/matches/swipes", new SwipeRequest(b.Session.UserId, "like"));
        var result = await b.Client.PostAsJsonAsync("/api/matches/swipes", new SwipeRequest(a.Session.UserId, "like"));
        return (a.Client, b.Client, (await result.Content.ReadFromJsonAsync<SwipeResponse>())!.MatchId!.Value);
    }
    private static GymRequest GymInput() { return new GymRequest { Slug="test-"+Guid.NewGuid().ToString("N"), Name="Fictional test gym", Address="Public test venue", AreaCode="0405802000", Latitude=14.6, Longitude=121.2, SessionPrice=500, SessionDurationMinutes=60 }; }
    private async Task<(HttpClient Owner, GymResponse Gym)> GymAsync()
    {
        var owner = await AccountAsync("gymOwner"); var created = await owner.Client.PostAsJsonAsync("/api/gyms", GymInput());
        Assert.Equal(HttpStatusCode.OK, created.StatusCode); var gym=(await created.Content.ReadFromJsonAsync<GymResponse>())!;
        gym=(await (await owner.Client.PostAsJsonAsync($"/api/gyms/{gym.Id}/publish",new{})).Content.ReadFromJsonAsync<GymResponse>())!;
        return (owner.Client,gym);
    }
    private async Task<FightResponse> RequestAsync(HttpClient client, long matchId, GymResponse gym, DateTimeOffset time)
    {
        var result = await client.PostAsJsonAsync("/api/fights",new ScheduleRequest { MatchId=matchId, GymId=gym.Id, GymVersion=gym.Version, ScheduledAt=time });
        Assert.Equal(HttpStatusCode.OK,result.StatusCode);return (await result.Content.ReadFromJsonAsync<FightResponse>())!;
    }
    [Fact]
    public async Task RoleSelectionPreventsEscalationAndOwnerDiscovery_AndOwnerCanEnableFighter()
    {
        var owner=await AccountAsync("gymOwner");using var o=owner.Client;
        Assert.Equal(new[]{"GYM_OWNER"},owner.Session.Roles);Assert.DoesNotContain("user_canMatch",owner.Session.Permissions);
        var fighter=await AccountAsync();using var f=fighter.Client;
        var discover=(await f.GetFromJsonAsync<List<ProfileResponse>>("/api/profiles/discover"))!;
        Assert.DoesNotContain(discover,p=>p.UserId==owner.Session.UserId);
        Assert.Equal(HttpStatusCode.NotFound,(await f.PostAsJsonAsync("/api/matches/swipes",new SwipeRequest(owner.Session.UserId,"like"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await o.GetAsync("/api/matches")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await f.PostAsJsonAsync("/api/auth/enable-fighter",new{})).StatusCode);
        var enabled=await o.PostAsJsonAsync("/api/auth/enable-fighter",new{});Assert.Equal(HttpStatusCode.OK,enabled.StatusCode);
        Assert.Contains("FIGHTER",(await enabled.Content.ReadFromJsonAsync<AuthResponse>())!.Roles);
        using var anonymous=Client();await CsrfAsync(anonymous);
        Assert.Equal(HttpStatusCode.BadRequest,(await anonymous.PostAsJsonAsync("/api/auth/register",new RegisterRequest("bad@example.test","Practice123!Example","Bad") { AccountType="ADMIN" })).StatusCode);
    }
    [Fact]
    public async Task PublicPinsAreExactAndOwnerDraftsArePrivate_WithSafeSeedHistory()
    {
        using var anonymous=Client();var gyms=(await anonymous.GetFromJsonAsync<List<GymResponse>>("/api/gyms"))!;
        var demos=gyms.Where(g=>g.IsDemo).OrderBy(g=>g.Name).ToList();Assert.Equal(3,demos.Count);
        Assert.Equal(121.1872028,demos[0].Longitude);Assert.Equal(14.5855941,demos[0].Latitude);
        Assert.Equal(121.18830765431449,demos[1].Longitude);Assert.Equal(121.18684478334319,demos[2].Longitude);
        foreach(var gym in demos)
        {
            var json=await anonymous.GetFromJsonAsync<JsonElement>($"/api/gyms/{gym.Id}");Assert.Equal(1,json.GetProperty("fights").GetArrayLength());
            Assert.DoesNotContain("emergencyContact",json.GetRawText());Assert.DoesNotContain("matchId",json.GetRawText());Assert.DoesNotContain("Seed-Alex",json.GetRawText());
        }
        var account=await AccountAsync("gymOwner");using var owner=account.Client;
        var created=await owner.PostAsJsonAsync("/api/gyms",GymInput());var draft=(await created.Content.ReadFromJsonAsync<GymResponse>())!;
        Assert.Equal(HttpStatusCode.NotFound,(await anonymous.GetAsync($"/api/gyms/{draft.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await anonymous.GetAsync($"/api/gyms/{draft.Id}/cover")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await anonymous.GetAsync($"/api/media/gyms/{draft.Id}/cover")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await owner.GetAsync($"/api/gyms/{draft.Id}")).StatusCode);
        owner.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");Assert.Equal(HttpStatusCode.BadRequest,(await owner.PostAsJsonAsync($"/api/gyms/{draft.Id}/publish",new{})).StatusCode);
    }
    [Fact]
    public async Task CompleteGymFlowKeepsWaiversPrivate_AndRequiresBothPublicationConsents()
    {
        var g=await GymAsync();using var owner=g.Owner;var p=await PairAsync();using var a=p.A;using var b=p.B;using var publicClient=Client();
        var start=_app.Clock.GetUtcNow().AddDays(2);var fight=await RequestAsync(a,p.MatchId,g.Gym,start);
        Assert.Equal("pending_approval",fight.Status);Assert.Equal(500,fight.QuotedPrice);
        Assert.Equal(HttpStatusCode.Conflict,(await a.PostAsJsonAsync($"/api/fights/{fight.Id}/confirm",new ConfirmRequest(true,"Private contact","000"))).StatusCode);
        var other=await AccountAsync("gymOwner");using var outsider=other.Client;
        Assert.Equal(HttpStatusCode.NotFound,(await outsider.PostAsJsonAsync($"/api/gyms/bookings/{fight.Id}/approve",new{})).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await owner.PostAsJsonAsync($"/api/gyms/bookings/{fight.Id}/approve",new{})).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await a.PostAsJsonAsync($"/api/fights/{fight.Id}/confirm",new ConfirmRequest(true,"Private contact A","000") { AllowPublicIdentity=true })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await b.PostAsJsonAsync($"/api/fights/{fight.Id}/confirm",new ConfirmRequest(true,"Private contact B","111"))).StatusCode);
        string bookings=await owner.GetStringAsync("/api/gyms/bookings");Assert.DoesNotContain("Private contact",bookings);Assert.DoesNotContain("acceptedAt",bookings);
        Assert.Equal(HttpStatusCode.Forbidden,(await owner.GetAsync($"/api/fights/{fight.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await a.PostAsJsonAsync($"/api/fights/{fight.Id}/complete",new{})).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await owner.PostAsJsonAsync($"/api/gyms/bookings/{fight.Id}/complete",new{})).StatusCode);
        DateTimeOffset old=_app.Clock.Now;
        try
        {
            _app.Clock.Now=start.AddMinutes(61);
            Assert.Equal(HttpStatusCode.OK,(await owner.PostAsJsonAsync($"/api/gyms/bookings/{fight.Id}/complete",new{})).StatusCode);
            var history=(await publicClient.GetFromJsonAsync<GymResponse>($"/api/gyms/{g.Gym.Id}"))!;Assert.Null(Assert.Single(history.Fights).Fighter1Name);
            Assert.Equal(HttpStatusCode.OK,(await b.PostAsJsonAsync($"/api/fights/{fight.Id}/publication",new PublicationRequest { AllowPublicIdentity=true })).StatusCode);
            history=(await publicClient.GetFromJsonAsync<GymResponse>($"/api/gyms/{g.Gym.Id}"))!;Assert.NotNull(history.Fights[0].Fighter1Name);Assert.NotNull(history.Fights[0].Fighter2Name);
            await a.PostAsJsonAsync($"/api/fights/{fight.Id}/publication",new PublicationRequest { AllowPublicIdentity=false });
            history=(await publicClient.GetFromJsonAsync<GymResponse>($"/api/gyms/{g.Gym.Id}"))!;Assert.Null(history.Fights[0].Fighter1Name);
            string json=await publicClient.GetStringAsync($"/api/gyms/{g.Gym.Id}");Assert.DoesNotContain("Private contact",json);Assert.DoesNotContain("userId",json);
        }
        finally { _app.Clock.Now=old; }
    }
    [Fact]
    public async Task ConcurrentOverlappingApprovalsAllowOne_AdjacentAllowed_AndCancellationReleasesSpace()
    {
        var g=await GymAsync();using var owner=g.Owner;var p=await PairAsync();var q=await PairAsync();var r=await PairAsync();
        using var a=p.A;using var b=p.B;using var c=q.A;using var d=q.B;using var e=r.A;using var f=r.B;
        var start=_app.Clock.GetUtcNow().AddDays(4);var first=await RequestAsync(a,p.MatchId,g.Gym,start);var second=await RequestAsync(c,q.MatchId,g.Gym,start.AddMinutes(30));
        var responses=await Task.WhenAll(owner.PostAsJsonAsync($"/api/gyms/bookings/{first.Id}/approve",new{}),owner.PostAsJsonAsync($"/api/gyms/bookings/{second.Id}/approve",new{}));
        Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.OK);Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Conflict);
        long winner=responses[0].IsSuccessStatusCode?first.Id:second.Id;var end=responses[0].IsSuccessStatusCode?start.AddMinutes(-60):start.AddMinutes(90);
        var adjacent=await RequestAsync(e,r.MatchId,g.Gym,end);Assert.Equal(HttpStatusCode.OK,(await owner.PostAsJsonAsync($"/api/gyms/bookings/{adjacent.Id}/approve",new{})).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await owner.PostAsJsonAsync($"/api/gyms/bookings/{winner}/cancel",new CancelRequest("Space released"))).StatusCode);
        long loser=winner==first.Id?second.Id:first.Id;Assert.Equal(HttpStatusCode.OK,(await owner.PostAsJsonAsync($"/api/gyms/bookings/{loser}/approve",new{})).StatusCode);
    }
    [Fact]
    public async Task QuotesStayFixed_LocationLocks_AndUnpublishingStopsNewRequests()
    {
        var g=await GymAsync();using var owner=g.Owner;var p=await PairAsync();using var a=p.A;using var b=p.B;
        var fight=await RequestAsync(a,p.MatchId,g.Gym,_app.Clock.GetUtcNow().AddDays(5));
        var input=GymInput();input.Slug=g.Gym.Slug;input.Version=g.Gym.Version;input.Name=g.Gym.Name;input.SessionPrice=900;
        var update=await owner.PutAsJsonAsync($"/api/gyms/{g.Gym.Id}",input);Assert.Equal(HttpStatusCode.OK,update.StatusCode);
        var updated=(await update.Content.ReadFromJsonAsync<GymResponse>())!;
        Assert.Equal(500,(await a.GetFromJsonAsync<FightResponse>($"/api/fights/{fight.Id}"))!.QuotedPrice);
        input.Version=updated.Version;input.Latitude=14.7;Assert.Equal(HttpStatusCode.Conflict,(await owner.PutAsJsonAsync($"/api/gyms/{g.Gym.Id}",input)).StatusCode);
        await owner.PostAsJsonAsync($"/api/gyms/{g.Gym.Id}/unpublish",new{});
        Assert.Equal(HttpStatusCode.OK,(await a.GetAsync($"/api/gyms/{g.Gym.Id}")).StatusCode);
        using var visitor=Client();Assert.Equal(HttpStatusCode.NotFound,(await visitor.GetAsync($"/api/gyms/{g.Gym.Id}")).StatusCode);
        await owner.PostAsJsonAsync($"/api/gyms/bookings/{fight.Id}/decline",new CancelRequest("Closed"));
        Assert.Equal(HttpStatusCode.Conflict,(await a.PostAsJsonAsync("/api/fights",new ScheduleRequest { MatchId=p.MatchId,GymId=g.Gym.Id,GymVersion=updated.Version,ScheduledAt=_app.Clock.GetUtcNow().AddDays(6) })).StatusCode);
        var privateFight=await a.PostAsJsonAsync("/api/fights",new ScheduleRequest(p.MatchId,_app.Clock.GetUtcNow().AddDays(6),"Private venue","Private address"));Assert.Equal(HttpStatusCode.OK,privateFight.StatusCode);
    }
    [Fact]
    public async Task ExpiredAndDuplicateRequestsRequireCancellation_StaleQuotesAndForeignEditsAreRejected()
    {
        var g=await GymAsync();using var owner=g.Owner;var p=await PairAsync();using var a=p.A;using var b=p.B;
        var start=_app.Clock.GetUtcNow().AddDays(1);var fight=await RequestAsync(a,p.MatchId,g.Gym,start);
        Assert.Equal(HttpStatusCode.Conflict,(await a.PostAsJsonAsync("/api/fights",new ScheduleRequest(p.MatchId,start,"Private","Private address"))).StatusCode);
        var other=await AccountAsync("gymOwner");using var o=other.Client;var input=GymInput();input.Version=g.Gym.Version;
        Assert.Equal(HttpStatusCode.NotFound,(await o.PutAsJsonAsync($"/api/gyms/{g.Gym.Id}",input)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await o.PostAsJsonAsync($"/api/gyms/{g.Gym.Id}/publish",new{})).StatusCode);
        DateTimeOffset old=_app.Clock.Now;
        try { _app.Clock.Now=start.AddMinutes(1);Assert.Equal(HttpStatusCode.Conflict,(await owner.PostAsJsonAsync($"/api/gyms/bookings/{fight.Id}/approve",new{})).StatusCode); }
        finally { _app.Clock.Now=old; }
        Assert.Equal(HttpStatusCode.OK,(await b.PostAsJsonAsync($"/api/fights/{fight.Id}/cancel",new CancelRequest("Expired request canceled"))).StatusCode);
        input.Name=g.Gym.Name;input.Slug=g.Gym.Slug;input.SessionPrice=750;
        Assert.Equal(HttpStatusCode.OK,(await owner.PutAsJsonAsync($"/api/gyms/{g.Gym.Id}",input)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await a.PostAsJsonAsync("/api/fights",new ScheduleRequest {MatchId=p.MatchId,GymId=g.Gym.Id,GymVersion=g.Gym.Version,ScheduledAt=start})).StatusCode);
    }

}
