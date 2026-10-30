using System.Net;
using System.Net.Http.Json;
using Lalamog.Api.DTOs;
using Lalamog.Api.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Lalamog.Api.Tests;

public class ServiceCompositionTests
{
    [Fact]
    public async Task AuthenticationServiceCanBeReplacedWithoutSql_WhileCsrfStillApplies()
    {
        using var app = new ServiceOverrideFactory();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        var request = new LoginRequest("interface-test@example.test", "Practice123!Example");
        var rejected = await client.PostAsJsonAsync("/api/auth/login", request);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal(0, app.Auth.LoginCalls);

        await PrepareCsrfAsync(client);
        var accepted = await client.PostAsJsonAsync("/api/auth/login", request);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        var response = await accepted.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(app.Auth.UserId, response!.UserId);
        Assert.Equal(1, app.Auth.LoginCalls);

        // Resolve the production graph, including service-to-service interfaces.
        // Merely constructing these services must not open a SQL connection.
        using var scope = app.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IProfileService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IMatchingService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IConversationService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IFightService>());
    }

    [Theory]
    [InlineData("missing-email")]
    [InlineData("invalid-email")]
    [InlineData("short-password")]
    [InlineData("long-password")]
    [InlineData("short-nickname")]
    [InlineData("long-nickname")]
    public async Task RegistrationDtoValidationRejectsInvalidInputBeforeCallingService(string invalidField)
    {
        using var app = new ServiceOverrideFactory();
        using var client = app.CreateClient();
        await PrepareCsrfAsync(client);

        var request = new Dictionary<string, string>
        {
            ["email"] = "interface-test@example.test",
            ["password"] = "Practice123!Example",
            ["nickname"] = "Fighter"
        };

        switch (invalidField)
        {
            case "missing-email":
                request.Remove("email");
                break;
            case "invalid-email":
                request["email"] = "not-an-email";
                break;
            case "short-password":
                request["password"] = "short";
                break;
            case "long-password":
                request["password"] = new string('a', 129);
                break;
            case "short-nickname":
                request["nickname"] = "x";
                break;
            case "long-nickname":
                request["nickname"] = new string('x', 51);
                break;
        }

        var response = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, app.Auth.RegisterCalls);
    }

    private static async Task PrepareCsrfAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", response!.Token);
    }

    private sealed class ServiceOverrideFactory : WebApplicationFactory<Program>
    {
        private readonly string _directory = Path.Combine(
            Path.GetTempPath(), "LalamogInterfaceTests_" + Guid.NewGuid().ToString("N"));

        public StubAuthService Auth { get; } = new StubAuthService();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Default", "Server=127.0.0.1,1;Database=NotUsed;Integrated Security=True;Connect Timeout=1");
            builder.UseSetting("Storage:Path", Path.Combine(_directory, "uploads"));
            builder.UseSetting("DataProtection:Path", Path.Combine(_directory, "keys"));
            builder.ConfigureServices(ReplaceAuthService);
        }

        private void ReplaceAuthService(IServiceCollection services)
        {
            services.RemoveAll<IAuthService>();
            services.AddSingleton<IAuthService>(Auth);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && Directory.Exists(_directory))
            {
                string temporaryRoot = Path.GetFullPath(Path.GetTempPath())
                    .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string resolved = Path.GetFullPath(_directory);
                if (!resolved.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase)
                    || !Path.GetFileName(resolved).StartsWith("LalamogInterfaceTests_", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Refusing to delete an unexpected test directory.");
                }
                Directory.Delete(resolved, recursive: true);
            }
        }
    }

    private sealed class StubAuthService : IAuthService
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public int LoginCalls { get; private set; }
        public int RegisterCalls { get; private set; }

        public Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            RegisterCalls++;
            return Task.FromResult(new AuthResponse { UserId = UserId });
        }

        public Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            LoginCalls++;
            return Task.FromResult(new AuthResponse { UserId = UserId });
        }

        public Task LogoutAsync()
        {
            return Task.CompletedTask;
        }
    }
}
