using System.Net;
using System.Net.Http.Json;
using Lalamog.Api.Authorization;
using Lalamog.Api.Data;
using Lalamog.Api.DTOs;
using Lalamog.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lalamog.Api.Tests;

public class PermissionContainerTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _app;
    public PermissionContainerTests(ApiFixture app) { _app = app; }

    private HttpClient Client()
    {
        return _app.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true, AllowAutoRedirect = false });
    }

    private static async Task CsrfAsync(HttpClient client)
    {
        var csrf = (await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf"))!;
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.Token);
    }

    private async Task<(HttpClient Client, Guid Id)> RegisterAsync()
    {
        var client = Client();
        await CsrfAsync(client);
        var result = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(Guid.NewGuid().ToString("N") + "@example.test", "Practice123!Example", "Permission tester"));
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var session = (await result.Content.ReadFromJsonAsync<AuthResponse>())!;
        await CsrfAsync(client);
        return (client, session.UserId);
    }

    private async Task<int> AssignCustomRoleAsync(Guid userId, params string[] codes)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var role = new Role { Name = "Custom-" + Guid.NewGuid().ToString("N") };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        var previous = await (from link in db.UserRoles where link.UserId == userId select link).ToListAsync();
        db.UserRoles.RemoveRange(previous);
        db.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id });
        foreach (string code in codes)
        {
            int featureId = await (from feature in db.SystemFeatures where feature.Code == code select feature.Id).SingleAsync();
            db.RoleSystemFeatures.Add(new RoleSystemFeature { RoleId = role.Id, SystemFeatureId = featureId });
        }
        await db.SaveChangesAsync();
        return role.Id;
    }

    [Fact]
    public async Task CustomRoleGrantsGymOperations_AndRevocationUsesCurrentCookie()
    {
        var account = await RegisterAsync();
        using var client = account.Client;
        int roleId = await AssignCustomRoleAsync(account.Id, ApplicationPermissions.Owner_ManageGyms);
        var request = new GymRequest { Slug = "permission-" + Guid.NewGuid().ToString("N"), Name = "Custom permission gym", Address = "Fictional public address", AreaCode = "0405802000", Latitude = 14.6, Longitude = 121.2, SessionPrice = 500 };
        var created = await client.PostAsJsonAsync("/api/gyms", request);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var gym = (await created.Content.ReadFromJsonAsync<GymResponse>())!;
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/gyms/{gym.Id}/publish", new { })).StatusCode);
        using var anonymous = Client();
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync($"/api/gyms/{gym.Id}")).StatusCode);
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var grant = await (from link in db.RoleSystemFeatures where link.RoleId == roleId select link).SingleAsync();
            db.RoleSystemFeatures.Remove(grant);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/gyms/mine")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/gyms/{gym.Id}")).StatusCode);
        var session = (await client.GetFromJsonAsync<AuthResponse>("/api/auth/me"))!;
        Assert.Empty(session.Permissions);
        Assert.DoesNotContain("GYM_OWNER", session.Roles);
    }

    [Fact]
    public async Task MatchingEligibilityUsesPermission_AndFighterOptInRequiresItsOwnPermission()
    {
        var target = await RegisterAsync();
        using var targetClient = target.Client;
        int roleId = await AssignCustomRoleAsync(target.Id, ApplicationPermissions.User_View, ApplicationPermissions.User_Match);
        var actor = await RegisterAsync();
        using var actorClient = actor.Client;
        var discovery = (await actorClient.GetFromJsonAsync<List<ProfileResponse>>("/api/profiles/discover"))!;
        Assert.Contains(target.Id, from item in discovery select item.UserId);
        Assert.Equal(HttpStatusCode.OK, (await actorClient.PostAsJsonAsync("/api/matches/swipes", new SwipeRequest(target.Id, "like"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await targetClient.PostAsJsonAsync("/api/auth/enable-fighter", new { })).StatusCode);
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            int featureId = await (from feature in db.SystemFeatures where feature.Code == ApplicationPermissions.Owner_EnableFighter select feature.Id).SingleAsync();
            db.RoleSystemFeatures.Add(new RoleSystemFeature { RoleId = roleId, SystemFeatureId = featureId });
            await db.SaveChangesAsync();
        }
        var enabled = await targetClient.PostAsJsonAsync("/api/auth/enable-fighter", new { });
        Assert.Equal(HttpStatusCode.OK, enabled.StatusCode);
        var session = (await enabled.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Contains("FIGHTER", session.Roles);
        Assert.DoesNotContain("GYM_OWNER", session.Roles);
    }

    [Fact]
    public async Task VisitorIsAnonymousOnly_AndCannotAcquirePrivateAccessEvenWithAccidentalGrant()
    {
        using var anonymous = Client();
        await CsrfAsync(anonymous);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/api/gyms")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/profiles/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/gyms/bookings")).StatusCode);
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var visitor = await (from role in db.Roles where role.Name == "VISITOR" select role).SingleAsync();
        Assert.False(await (from link in db.UserRoles where link.RoleId == visitor.Id select link).AnyAsync());
        int manageId = await (from feature in db.SystemFeatures where feature.Code == ApplicationPermissions.Owner_ManageGyms select feature.Id).SingleAsync();
        var accidental = new RoleSystemFeature { RoleId = visitor.Id, SystemFeatureId = manageId };
        db.RoleSystemFeatures.Add(accidental);
        await db.SaveChangesAsync();
        try
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/gyms", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/gyms/mine")).StatusCode);
        }
        finally
        {
            db.RoleSystemFeatures.Remove(accidental);
            await db.SaveChangesAsync();
        }
        var publicGrant = await (from link in db.RoleSystemFeatures where link.RoleId == visitor.Id && link.SystemFeature.Code == ApplicationPermissions.Visitor_ViewGyms select link).SingleAsync();
        db.RoleSystemFeatures.Remove(publicGrant);
        await db.SaveChangesAsync();
        try
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/gyms")).StatusCode);
            await DatabaseSeeder.SeedAsync(db);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/gyms")).StatusCode);
            var fighter = await RegisterAsync();
            using var fighterClient = fighter.Client;
            Assert.Equal(HttpStatusCode.OK, (await fighterClient.GetAsync("/api/gyms")).StatusCode);
        }
        finally
        {
            db.RoleSystemFeatures.Add(new RoleSystemFeature { RoleId = publicGrant.RoleId, SystemFeatureId = publicGrant.SystemFeatureId });
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task VisitorCannotBeAssignedAsAnAccountRole()
    {
        using var admin = Client();
        await CsrfAsync(admin);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync("/api/auth/login", new LoginRequest("alex.seed@example.test", "Lalamog123!Test"))).StatusCode);
        await CsrfAsync(admin);
        var roles = (await admin.GetFromJsonAsync<List<RoleResponse>>("/api/access/roles"))!;
        foreach (string expected in new[] { "FIGHTER", "ADMIN", "GYM_OWNER", "VISITOR" })
        {
            Assert.Single(from role in roles where role.Name == expected select role);
        }
        var account = await RegisterAsync();
        using var client = account.Client;
        int visitorId = (from role in roles where role.Name == "VISITOR" select role.Id).Single();
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/access/users/{account.Id}/roles", new { roleIds = new[] { visitorId } })).StatusCode);
        var session = (await client.GetFromJsonAsync<AuthResponse>("/api/auth/me"))!;
        Assert.Equal(new[] { "FIGHTER" }, session.Roles);
    }
}
