using System.Net;
using System.Net.Http.Json;
using Lalamog.Api.Data;
using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lalamog.Api.Tests;

public class NativeAuthTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _app;
    private const string Password = "Practice123!Example";

    public NativeAuthTests(ApiFixture app)
    {
        _app = app;
    }

    private async Task<HttpClient> ClientAsync()
    {
        HttpClient client = _app.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true, AllowAutoRedirect = false });
        await RefreshCsrfAsync(client);
        return client;
    }

    private static async Task RefreshCsrfAsync(HttpClient client)
    {
        CsrfResponse? csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.Token);
    }

    [Fact]
    public async Task NativeRegistrationStoresHashAndSupportsCaseInsensitiveLogin()
    {
        using HttpClient client = await ClientAsync();
        string email = Guid.NewGuid().ToString("N") + "@example.test";
        HttpResponseMessage registered = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, Password, "Crow"));
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        AuthResponse? account = await registered.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(account!.UserId, (await client.GetFromJsonAsync<AuthResponse>("/api/auth/me"))!.UserId);
        using (IServiceScope scope = _app.Services.CreateScope())
        {
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FindAsync(account.UserId);
            Assert.NotNull(user);
            Assert.Equal(email.ToUpperInvariant(), user.NormalizedEmail);
            Assert.True(PasswordHashing.Verify(user.PasswordHash, Password));
            Assert.NotEqual(Password, user.PasswordHash);
            Assert.NotNull(await db.Profiles.FindAsync(account.UserId));
        }
        await RefreshCsrfAsync(client);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        await RefreshCsrfAsync(client);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email.ToUpperInvariant(), Password))).StatusCode);
    }

    [Fact]
    public async Task FailedLoginsLockTheAccountUntilTheClockAdvances()
    {
        string email = Guid.NewGuid().ToString("N") + "@example.test";
        using (HttpClient registration = await ClientAsync())
        {
            Assert.Equal(HttpStatusCode.OK, (await registration.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, Password, "Crow"))).StatusCode);
        }
        using HttpClient client = await ClientAsync();
        DateTimeOffset previous = _app.Clock.Now;
        try
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "wrong"))).StatusCode);
            }
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password))).StatusCode);
            _app.Clock.Now = previous.AddMinutes(6);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password))).StatusCode);
            using IServiceScope scope = _app.Services.CreateScope();
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var query = from user in db.Users where user.NormalizedEmail == email.ToUpperInvariant() select user;
            var stored = await query.SingleAsync();
            Assert.Equal(0, stored.AccessFailedCount);
            Assert.Null(stored.LockoutEnd);
        }
        finally
        {
            _app.Clock.Now = previous;
        }
    }

    [Fact]
    public async Task ConcurrentRegistrationCannotCreateDuplicateEmailAccounts()
    {
        string email = Guid.NewGuid().ToString("N") + "@example.test";
        using HttpClient first = await ClientAsync();
        using HttpClient second = await ClientAsync();
        HttpResponseMessage[] attempts = await Task.WhenAll(
            first.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, Password, "Crow")),
            second.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email.ToUpperInvariant(), Password, "Crow")));
        Assert.Single(attempts, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(attempts, response => response.StatusCode == HttpStatusCode.BadRequest);
        using IServiceScope scope = _app.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var query = from user in db.Users where user.NormalizedEmail == email.ToUpperInvariant() select user;
        Assert.Equal(1, await query.CountAsync());
    }

    [Fact]
    public async Task NativeRegistrationRetainsPasswordComplexityValidation()
    {
        using HttpClient client = await ClientAsync();
        string email = Guid.NewGuid().ToString("N") + "@example.test";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "onlylowercase", "Crow"))).StatusCode);
    }
}
