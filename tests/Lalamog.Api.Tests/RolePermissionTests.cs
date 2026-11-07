using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Lalamog.Api.Authorization;
using Lalamog.Api.Data;
using Lalamog.Api.DTOs;
using Lalamog.Api.Models;
using Lalamog.Api.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lalamog.Api.Tests;

public class RolePermissionTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _app;

    public RolePermissionTests(ApiFixture app)
    {
        _app = app;
    }

    [Fact]
    public async Task AdminControllerRequiresPermission_AndAssignmentsChangeWithoutSigningInAgain()
    {
        using HttpClient anonymous = Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/access/system-features")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/api/areas/regions")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync("/api/media/" + new string('a', 32) + ".png")).StatusCode);

        using HttpClient user = Client();
        Guid userId = await RegisterAsync(user);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/access/roles")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/profiles/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.PutAsJsonAsync($"/api/access/users/{userId}/roles", new { roleIds = new[] { 1 } })).StatusCode);

        using HttpClient admin = await AdminAsync();
        var features = (await admin.GetFromJsonAsync<List<SystemFeatureResponse>>("/api/access/system-features"))!;
        Assert.Equal(13, features.Count);
        Assert.Equal(13, (from feature in features select feature.Code).Distinct().Count());
        List<RoleResponse> roles = (await admin.GetFromJsonAsync<List<RoleResponse>>("/api/access/roles"))!;
        RoleResponse standard = Assert.Single(from role in roles where role.Name == "FIGHTER" select role);
        RoleResponse administrator = Assert.Single(from role in roles where role.Name == "ADMIN" select role);
        Assert.Equal(8, standard.SystemFeatures.Count);
        Assert.Equal(3, administrator.SystemFeatures.Count);
        var original = (await admin.GetFromJsonAsync<UserRolesResponse>($"/api/access/users/{userId}/roles"))!;
        Assert.Equal(new[] { standard.Id }, original.RoleIds);

        admin.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/access/users/{userId}/roles", new { roleIds = new[] { standard.Id, administrator.Id } })).StatusCode);
        await CsrfAsync(admin);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/access/users/{userId}/roles", new { roleIds = new[] { standard.Id, administrator.Id } })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/access/system-features")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/profiles/me")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/access/users/{userId}/roles", new { roleIds = new[] { standard.Id } })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/access/system-features")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/access/users/{userId}/roles", new { roleIds = Array.Empty<int>() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/profiles/me")).StatusCode);
        // Authentication and logout remain usable even after every app permission is removed.
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await user.PostAsJsonAsync("/api/auth/logout", new { })).StatusCode);
    }

    [Fact]
    public async Task FeatureGrantsAreCombinedAcrossRoles_AndControllerAndActionBothRequirePermission()
    {
        using HttpClient user = Client();
        Guid userId = await RegisterAsync(user);
        using HttpClient admin = await AdminAsync();
        List<SystemFeatureResponse> features = (await admin.GetFromJsonAsync<List<SystemFeatureResponse>>("/api/access/system-features"))!;
        int view = Assert.Single(from feature in features where feature.Code == ApplicationPermissions.User_View select feature.Id);
        int upload = Assert.Single(from feature in features where feature.Code == ApplicationPermissions.User_Upload select feature.Id);
        int viewerId;
        int uploaderId;
        using (var scope = _app.Services.CreateScope())
        {
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var viewer = new Role { Name = "Viewer-" + Guid.NewGuid().ToString("N") };
            var uploader = new Role { Name = "Uploader-" + Guid.NewGuid().ToString("N") };
            db.Roles.AddRange(viewer, uploader);
            await db.SaveChangesAsync();
            viewerId = viewer.Id;
            uploaderId = uploader.Id;
        }
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/access/roles/{viewerId}/system-features", new { systemFeatureIds = new[] { view } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/access/roles/{uploaderId}/system-features", new { systemFeatureIds = new[] { upload } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/access/users/{userId}/roles", new { roleIds = new[] { viewerId } })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/profiles/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, await UploadEmptyAsync(user));
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/access/users/{userId}/roles", new { roleIds = new[] { viewerId, uploaderId } })).StatusCode);
        // Both policies passed; MVC now rejects the deliberately missing file.
        Assert.Equal(HttpStatusCode.BadRequest, await UploadEmptyAsync(user));
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/access/roles/{uploaderId}/system-features", new { systemFeatureIds = Array.Empty<int>() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, await UploadEmptyAsync(user));
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/access/roles/{uploaderId}/system-features", new { systemFeatureIds = new[] { upload } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/access/users/{userId}/roles", new { roleIds = new[] { uploaderId } })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, await UploadEmptyAsync(user));
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/profiles/me")).StatusCode);
    }

    [Fact]
    public async Task InvalidAssignmentsAreRejectedWithoutChangingExistingGrants()
    {
        using HttpClient admin = await AdminAsync();
        using HttpClient user = Client();
        Guid userId = await RegisterAsync(user);
        var original = (await admin.GetFromJsonAsync<UserRolesResponse>($"/api/access/users/{userId}/roles"))!;
        int roleId = Assert.Single(original.RoleIds);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/access/users/{userId}/roles", new { roleIds = new[] { int.MaxValue } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/access/users/{userId}/roles", new { roleIds = new[] { roleId, roleId } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/access/users/{userId}/roles", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync($"/api/access/users/{Guid.NewGuid()}/roles", new { roleIds = new[] { roleId } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/access/roles/{roleId}/system-features", new { systemFeatureIds = new[] { int.MaxValue } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync($"/api/access/roles/{int.MaxValue}/system-features", new { systemFeatureIds = Array.Empty<int>() })).StatusCode);
        Assert.Equal(original.RoleIds, (await admin.GetFromJsonAsync<UserRolesResponse>($"/api/access/users/{userId}/roles"))!.RoleIds);
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/profiles/me")).StatusCode);
    }

    [Fact]
    public async Task HandlerRejectsMissingMalformedUnknownUsersAndUnknownFeatures()
    {
        using var scope = _app.Services.CreateScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var malformed = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") }, "test"));
        Assert.False((await authorization.AuthorizeAsync(new ClaimsPrincipal(), null, ApplicationPermissions.User_View)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(malformed, null, ApplicationPermissions.User_View)).Succeeded);
        var absent = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "test"));
        Assert.False((await authorization.AuthorizeAsync(absent, null, ApplicationPermissions.User_View)).Succeeded);
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Guid alexId = await (from account in db.Users where account.Email == "alex.seed@example.test" select account.Id).SingleAsync();
        var alex = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, alexId.ToString()) }, "test"));
        Assert.False((await authorization.AuthorizeAsync(alex, null, new[] { new SystemFeatureRequirement("admin_all") })).Succeeded);
    }

    private HttpClient Client()
    {
        return _app.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true, AllowAutoRedirect = false });
    }

    private async Task<HttpClient> AdminAsync()
    {
        HttpClient client = Client();
        await CsrfAsync(client);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("alex.seed@example.test", "Lalamog123!Test"))).StatusCode);
        await CsrfAsync(client);
        return client;
    }

    private static async Task<Guid> RegisterAsync(HttpClient client)
    {
        await CsrfAsync(client);
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = Guid.NewGuid().ToString("N") + "@example.test",
            password = "Practice123!Example", nickname = "Permission tester",
            roles = new[] { "ADMIN" }, roleIds = new[] { 1 }, systemFeatureIds = new[] { 1 }
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Guid id = (await response.Content.ReadFromJsonAsync<AuthResponse>())!.UserId;
        await CsrfAsync(client);
        return id;
    }

    private static async Task CsrfAsync(HttpClient client)
    {
        var csrf = (await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf"))!;
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.Token);
    }

    private static async Task<HttpStatusCode> UploadEmptyAsync(HttpClient client)
    {
        using var body = new MultipartFormDataContent();
        return (await client.PostAsync("/api/profiles/me/avatar", body)).StatusCode;
    }
}
