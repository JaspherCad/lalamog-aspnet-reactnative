using System.Threading.RateLimiting;
using Lalamog.Api.Data;
using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Repositories;
using Lalamog.Api.Services;
using Lalamog.Api.Requirements;
using Lalamog.Api.Authorization;
using Lalamog.Api.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));


builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "lalamog.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    });

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "lalamog.csrf";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});




/*PERSONAL PSEUDOCODE DO NOT REMOVE!
1: create general SystemFeatureRequirement : IAuthorizationRequirement
    that accepts string Role;

2: builder.Services.AddAuthorizationBuilder()
    .AddPolicy(ApplicationPermissions.User_View, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new SystemFeatureRequirement(ApplicationPermissions.User_View)))
    .AddPolicy(ApplicationPermissions.User_EditProfile, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new SystemFeatureRequirement(ApplicationPermissions.User_EditProfile)))
    .AddPolicy(ApplicationPermissions.User_Upload, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new SystemFeatureRequirement(ApplicationPermissions.User_Upload)))

3: create Handler SystemPermissionHandler : AuthorizationHandler<SystemFeatureRequirement>

4: builder.Services.AddScoped<IAuthorizationHandler, SystemPermissionHandler>();


IAuthReqs and Handlers are accessible because of AddAuthorizationBuilder


 */
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(ApplicationPermissions.User_View, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new SystemFeatureRequirement(ApplicationPermissions.User_View)))
    .AddPolicy(ApplicationPermissions.User_EditProfile, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new SystemFeatureRequirement(ApplicationPermissions.User_EditProfile)))
    .AddPolicy(ApplicationPermissions.User_Upload, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new SystemFeatureRequirement(ApplicationPermissions.User_Upload)))
    .AddPolicy(ApplicationPermissions.User_Match, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new SystemFeatureRequirement(ApplicationPermissions.User_Match)))
    .AddPolicy(ApplicationPermissions.User_Chat, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new SystemFeatureRequirement(ApplicationPermissions.User_Chat)))
    .AddPolicy(ApplicationPermissions.User_Schedule, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new SystemFeatureRequirement(ApplicationPermissions.User_Schedule)))
    .AddPolicy(ApplicationPermissions.User_Map, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new SystemFeatureRequirement(ApplicationPermissions.User_Map)))
    .AddPolicy(ApplicationPermissions.Admin_ManageAccess, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new SystemFeatureRequirement(ApplicationPermissions.Admin_ManageAccess)));

builder.Services.AddScoped<IAuthorizationHandler, SystemPermissionHandler>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

// MVC supplies the shared mutation antiforgery filter. React serves the UI.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<IAreaService, AreaService>();
builder.Services.AddMemoryCache(options => options.SizeLimit = 500);
builder.Services.AddHttpClient("Nominatim", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Maps:NominatimUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(8);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Lalamog/1.0");
}).RemoveAllLoggers(); // Provider URLs contain private coordinates.
builder.Services.AddSingleton<IReverseGeocodingService>(services => new ReverseGeocodingService(
    services.GetRequiredService<IHttpClientFactory>(), services.GetRequiredService<IMemoryCache>(),
    TimeSpan.FromSeconds(1), TimeSpan.FromDays(1)));
builder.Services.AddScoped<IAccessRepository, AccessRepository>();
builder.Services.AddScoped<IAccessService, AccessService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
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
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

// Rebuild a fresh local database and fill its demo data before accepting requests.
if (app.Environment.IsDevelopment())
{
    using (var serviceScope = app.Services.CreateScope())
    {
        var services = serviceScope.ServiceProvider;
        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(db);
    }
}

app.UseExceptionHandler();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapOpenApi("/api/openapi/{documentName}.json").AllowAnonymous();
app.MapGet("/health", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    if (await db.Database.CanConnectAsync(cancellationToken))
    {
        return (IResult)Results.Ok(new HealthResponse { Status = "ok" });
    }
    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});
app.Run();
