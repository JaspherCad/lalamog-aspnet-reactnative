using System.Threading.RateLimiting;
using Lalamog.Api.Data;
using Lalamog.Api.DTOs;
using Lalamog.Api.Hubs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Lalamog.Api.Repositories;
using Lalamog.Api.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
string? connection = builder.Configuration.GetConnectionString("Default");
if (connection == null)
{
    throw new InvalidOperationException("Set ConnectionStrings__Default. See scripts/dev.ps1.");
}

builder.Services.AddDbContext<AppDbContext>(ConfigureDatabase);
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(ConfigureIdentity)
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(ConfigureCookies);
builder.Services.AddAntiforgery(ConfigureAntiforgery);

string keysPath = builder.Configuration["DataProtection:Path"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "data", "keys");
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .SetApplicationName("Lalamog.Web")
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(ConfigureRateLimiting);

// MVC supplies the shared mutation antiforgery filter. React serves the UI.
builder.Services.AddControllersWithViews(ConfigureControllers);
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IMatchingRepository, MatchingRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IFightRepository, FightRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IMatchingService, MatchingService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IFightService, FightService>();
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();
if (args.Contains("--initialize"))
{
    await using var scope = app.Services.CreateAsyncScope();
    DatabaseInitializer initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync(CancellationToken.None);
    return;
}

app.UseExceptionHandler();
app.Use(ApplySecurityHeadersAsync);
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.Use(ValidateHubAntiforgeryAsync);
app.MapControllers();
app.MapHub<MessageHub>("/hubs/messages", ConfigureMessageHub);
app.MapOpenApi("/api/openapi/{documentName}.json").AllowAnonymous();
app.MapGet("/health", CheckDatabaseHealthAsync);
app.Run();

void ConfigureDatabase(DbContextOptionsBuilder options)
{
    options.UseSqlServer(connection, ConfigureSqlRetries);
}

static void ConfigureSqlRetries(SqlServerDbContextOptionsBuilder options)
{
    options.EnableRetryOnFailure(5, TimeSpan.FromSeconds(2), new[] { 1205 });
}

static void ConfigureIdentity(IdentityOptions options)
{
    options.Password.RequiredLength = 10;
    options.User.RequireUniqueEmail = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
}

void ConfigureCookies(CookieAuthenticationOptions options)
{
    options.Cookie.Name = "lalamog.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    if (builder.Configuration.GetValue<bool>("Cookies:Secure"))
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
    else
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    }
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = ReturnUnauthorizedAsync;
    options.Events.OnRedirectToAccessDenied = ReturnForbiddenAsync;
}

static Task ReturnUnauthorizedAsync(RedirectContext<CookieAuthenticationOptions> context)
{
    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    return Task.CompletedTask;
}

static Task ReturnForbiddenAsync(RedirectContext<CookieAuthenticationOptions> context)
{
    context.Response.StatusCode = StatusCodes.Status403Forbidden;
    return Task.CompletedTask;
}

void ConfigureAntiforgery(AntiforgeryOptions options)
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "lalamog.csrf";
    options.Cookie.SameSite = SameSiteMode.Strict;
    if (builder.Configuration.GetValue<bool>("Cookies:Secure"))
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
    else
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    }
}

static void ConfigureRateLimiting(RateLimiterOptions options)
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy<string>("auth", CreateAuthRateLimitPartition);
}

static RateLimitPartition<string> CreateAuthRateLimitPartition(HttpContext context)
{
    string key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return RateLimitPartition.GetFixedWindowLimiter(key, CreateAuthRateLimitOptions);
}

static FixedWindowRateLimiterOptions CreateAuthRateLimitOptions(string partitionKey)
{
    return new FixedWindowRateLimiterOptions
    {
        PermitLimit = 120,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0
    };
}

static void ConfigureControllers(MvcOptions options)
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
}

static async Task ApplySecurityHeadersAsync(HttpContext context, RequestDelegate next)
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    if (context.Request.Path.StartsWithSegments("/api") && !context.Request.Path.StartsWithSegments("/api/media"))
    {
        context.Response.Headers.CacheControl = "no-store";
    }

    if (context.Request.Path.StartsWithSegments("/hubs"))
    {
        string origin = context.Request.Headers.Origin.ToString();
        if (!string.IsNullOrEmpty(origin))
        {
            bool validOrigin = Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri);
            if (!validOrigin || !string.Equals(uri!.Authority, context.Request.Host.Value, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }
    }
    await next(context);
}

static async Task ValidateHubAntiforgeryAsync(HttpContext context, RequestDelegate next)
{
    if (HttpMethods.IsPost(context.Request.Method) && context.Request.Path.StartsWithSegments("/hubs"))
    {
        try
        {
            IAntiforgery antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
    }
    await next(context);
}

static void ConfigureMessageHub(HttpConnectionDispatcherOptions options)
{
    options.CloseOnAuthenticationExpiration = true;
}

static async Task<IResult> CheckDatabaseHealthAsync(AppDbContext db, CancellationToken cancellationToken)
{
    if (await db.Database.CanConnectAsync(cancellationToken))
    {
        return Results.Ok(new HealthResponse { Status = "ok" });
    }
    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
}

public partial class Program
{
}
