using System.Threading.RateLimiting;
using Lalamog.Api.Data;
using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Lalamog.Api.Repositories;
using Lalamog.Api.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options => options
    .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
    .UseSeeding((context, _) =>
    {
        var people = new[]
        {
            (Email: "alex.seed@example.test", Name: "Seed-Alex", Alias: "Iron", Style: "kickboxing", Level: 2, City: "Quezon City", Availability: "Saturday mornings"),
            (Email: "sam.seed@example.test", Name: "Seed-Sam", Alias: "Flow", Style: "bjj", Level: 2, City: "Manila", Availability: "Weekends"),
            (Email: "jordan.seed@example.test", Name: "Seed-Jordan", Alias: "Storm", Style: "wrestling", Level: 3, City: "Pasig", Availability: "Weekday evenings"),
            (Email: "miko.seed@example.test", Name: "Seed-Miko", Alias: "Crow", Style: "streetfighting|kickboxing", Level: 1, City: "Rizal", Availability: "Sunday afternoons"),
            (Email: "jae.seed@example.test", Name: "Seed-Jae", Alias: "Reaper", Style: "martial", Level: 2, City: "Makati", Availability: "Friday evenings"),
            (Email: "ana.seed@example.test", Name: "Seed-Ana", Alias: "Blaze", Style: "kickboxing|bjj", Level: 1, City: "Marikina", Availability: "Weekends")
        };
        var users = new List<AppUser>();
        var now = DateTimeOffset.UtcNow;

        // Save each parent first, then create its profile using the saved ID.
        foreach (var person in people)
        {
            var email = person.Email.ToUpperInvariant();
            var user = context.Set<AppUser>().FirstOrDefault(u => u.NormalizedEmail == email);
            if (user == null)
            {
                user = new AppUser
                {
                    Email = person.Email,
                    NormalizedEmail = email,
                    PasswordHash = PasswordHashing.Hash("Lalamog123!Test")
                };
                context.Set<AppUser>().Add(user);
                context.SaveChanges();
            }
            users.Add(user);

            if (!context.Set<FighterProfile>().Any(p => p.UserId == user.Id))
            {
                context.Set<FighterProfile>().Add(new FighterProfile
                {
                    UserId = user.Id,
                    FighterName = person.Name,
                    NormalizedFighterName = person.Name.ToUpperInvariant(),
                    Nickname = person.Alias,
                    Bio = "Fictional practice account for discovery, matching, chat and fight testing.",
                    FightingStyle = person.Style,
                    ExperienceLevel = person.Level,
                    Location = person.City,
                    Availability = person.Availability
                });
                context.SaveChanges();
            }
        }

        // Ana has no swipes/matches, so she remains available in discovery.
        var pairs = new[] { (Left: 0, Right: 1), (Left: 0, Right: 2), (Left: 0, Right: 3), (Left: 0, Right: 4), (Left: 1, Right: 2) };
        var matches = new List<FighterMatch>();
        foreach (var pair in pairs)
        {
            var left = users[pair.Left].Id;
            var right = users[pair.Right].Id;
            foreach (var swipe in new[] { (From: left, To: right), (From: right, To: left) })
            {
                if (!context.Set<Swipe>().Any(s => s.SwiperId == swipe.From && s.TargetId == swipe.To))
                {
                    context.Set<Swipe>().Add(new Swipe { SwiperId = swipe.From, TargetId = swipe.To, Direction = "like", CreatedAt = now.AddDays(-30) });
                    context.SaveChanges();
                }
            }
            if (string.CompareOrdinal(left.ToString("D"), right.ToString("D")) > 0)
            {
                (left, right) = (right, left);
            }
            var match = context.Set<FighterMatch>().FirstOrDefault(m => m.User1Id == left && m.User2Id == right);
            if (match == null)
            {
                match = new FighterMatch { User1Id = left, User2Id = right, CreatedAt = now.AddDays(-30) };
                context.Set<FighterMatch>().Add(match);
                context.SaveChanges();
            }
            matches.Add(match);

            foreach (var message in new[]
            {
                (Sender: users[pair.Left].Id, Text: "Practice message: ready for a friendly technical round?"),
                (Sender: users[pair.Right].Id, Text: "Practice reply: yes, let's choose a place and time.")
            })
            {
                if (!context.Set<ChatMessage>().Any(m => m.MatchId == match.Id && m.SenderId == message.Sender && m.Content == message.Text))
                {
                    context.Set<ChatMessage>().Add(new ChatMessage { MatchId = match.Id, SenderId = message.Sender, Content = message.Text, CreatedAt = now.AddDays(-29) });
                    context.SaveChanges();
                }
            }
        }

        var passer = users[2].Id;
        var passed = users[4].Id;
        if (!context.Set<Swipe>().Any(s => s.SwiperId == passer && s.TargetId == passed))
        {
            context.Set<Swipe>().Add(new Swipe { SwiperId = passer, TargetId = passed, Direction = "pass", CreatedAt = now.AddDays(-2) });
            context.SaveChanges();
        }

        // One unfinished fight per match; history can contain completed/canceled fights.
        var sessions = new[]
        {
            (Match: 0, Status: "completed", Days: -14, Venue: "Seed QC history court", Address: "Fictional test venue, Quezon City", Confirmations: 2),
            (Match: 0, Status: "confirmed", Days: 7, Venue: "Seed QC weekend court", Address: "Fictional test venue, Quezon City", Confirmations: 2),
            (Match: 1, Status: "scheduled", Days: 5, Venue: "Seed Pasig technique court", Address: "Fictional test venue, Pasig", Confirmations: 1),
            (Match: 2, Status: "completed", Days: -20, Venue: "Seed Rizal practice court", Address: "Fictional test venue, Rizal", Confirmations: 2),
            (Match: 3, Status: "canceled", Days: -7, Venue: "Seed Makati canceled court", Address: "Fictional test venue, Makati", Confirmations: 0),
            (Match: 4, Status: "confirmed", Days: -1, Venue: "Seed Manila completion court", Address: "Fictional test venue, Manila", Confirmations: 2)
        };
        foreach (var session in sessions)
        {
            var match = matches[session.Match];
            var fight = context.Set<Fight>().FirstOrDefault(f => f.MatchId == match.Id && f.Location == session.Venue);
            if (fight == null)
            {
                fight = new Fight
                {
                    MatchId = match.Id,
                    ScheduledAt = now.AddDays(session.Days),
                    Location = session.Venue,
                    Address = session.Address,
                    Status = session.Status,
                    CancellationReason = session.Status == "canceled" ? "Practice cancellation: venue unavailable." : null,
                    CreatedAt = now.AddDays(-28),
                    UpdatedAt = now.AddDays(-1)
                };
                context.Set<Fight>().Add(fight);
                context.SaveChanges();
            }
            var participants = new[] { match.User1Id, match.User2Id };
            for (var index = 0; index < session.Confirmations; index++)
            {
                var participant = participants[index];
                if (!context.Set<FightConfirmation>().Any(c => c.FightId == fight.Id && c.UserId == participant))
                {
                    context.Set<FightConfirmation>().Add(new FightConfirmation
                    {
                        FightId = fight.Id,
                        UserId = participant,
                        EmergencyContactName = "Fictional test contact",
                        EmergencyContactPhone = "000-000-0000",
                        AcceptedAt = fight.ScheduledAt < now ? fight.ScheduledAt.AddDays(-1) : now.AddDays(-1)
                    });
                    context.SaveChanges();
                }
            }
        }
    })
    .UseAsyncSeeding(async (context, _, cancellationToken) =>
    {
        var people = new[]
        {
            (Email: "alex.seed@example.test", Name: "Seed-Alex", Alias: "Iron", Style: "kickboxing", Level: 2, City: "Quezon City", Availability: "Saturday mornings"),
            (Email: "sam.seed@example.test", Name: "Seed-Sam", Alias: "Flow", Style: "bjj", Level: 2, City: "Manila", Availability: "Weekends"),
            (Email: "jordan.seed@example.test", Name: "Seed-Jordan", Alias: "Storm", Style: "wrestling", Level: 3, City: "Pasig", Availability: "Weekday evenings"),
            (Email: "miko.seed@example.test", Name: "Seed-Miko", Alias: "Crow", Style: "streetfighting|kickboxing", Level: 1, City: "Rizal", Availability: "Sunday afternoons"),
            (Email: "jae.seed@example.test", Name: "Seed-Jae", Alias: "Reaper", Style: "martial", Level: 2, City: "Makati", Availability: "Friday evenings"),
            (Email: "ana.seed@example.test", Name: "Seed-Ana", Alias: "Blaze", Style: "kickboxing|bjj", Level: 1, City: "Marikina", Availability: "Weekends")
        };
        var users = new List<AppUser>();
        var now = DateTimeOffset.UtcNow;

        // Save each parent first, then create its profile using the saved ID.
        foreach (var person in people)
        {
            var email = person.Email.ToUpperInvariant();
            var user = await context.Set<AppUser>().FirstOrDefaultAsync(u => u.NormalizedEmail == email, cancellationToken);
            if (user == null)
            {
                user = new AppUser
                {
                    Email = person.Email,
                    NormalizedEmail = email,
                    PasswordHash = PasswordHashing.Hash("Lalamog123!Test")
                };
                context.Set<AppUser>().Add(user);
                await context.SaveChangesAsync(cancellationToken);
            }
            users.Add(user);

            if (!(await context.Set<FighterProfile>().AnyAsync(p => p.UserId == user.Id, cancellationToken)))
            {
                context.Set<FighterProfile>().Add(new FighterProfile
                {
                    UserId = user.Id,
                    FighterName = person.Name,
                    NormalizedFighterName = person.Name.ToUpperInvariant(),
                    Nickname = person.Alias,
                    Bio = "Fictional practice account for discovery, matching, chat and fight testing.",
                    FightingStyle = person.Style,
                    ExperienceLevel = person.Level,
                    Location = person.City,
                    Availability = person.Availability
                });
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        // Ana has no swipes/matches, so she remains available in discovery.
        var pairs = new[] { (Left: 0, Right: 1), (Left: 0, Right: 2), (Left: 0, Right: 3), (Left: 0, Right: 4), (Left: 1, Right: 2) };
        var matches = new List<FighterMatch>();
        foreach (var pair in pairs)
        {
            var left = users[pair.Left].Id;
            var right = users[pair.Right].Id;
            foreach (var swipe in new[] { (From: left, To: right), (From: right, To: left) })
            {
                if (!(await context.Set<Swipe>().AnyAsync(s => s.SwiperId == swipe.From && s.TargetId == swipe.To, cancellationToken)))
                {
                    context.Set<Swipe>().Add(new Swipe { SwiperId = swipe.From, TargetId = swipe.To, Direction = "like", CreatedAt = now.AddDays(-30) });
                    await context.SaveChangesAsync(cancellationToken);
                }
            }
            if (string.CompareOrdinal(left.ToString("D"), right.ToString("D")) > 0)
            {
                (left, right) = (right, left);
            }
            var match = await context.Set<FighterMatch>().FirstOrDefaultAsync(m => m.User1Id == left && m.User2Id == right, cancellationToken);
            if (match == null)
            {
                match = new FighterMatch { User1Id = left, User2Id = right, CreatedAt = now.AddDays(-30) };
                context.Set<FighterMatch>().Add(match);
                await context.SaveChangesAsync(cancellationToken);
            }
            matches.Add(match);

            foreach (var message in new[]
            {
                (Sender: users[pair.Left].Id, Text: "Practice message: ready for a friendly technical round?"),
                (Sender: users[pair.Right].Id, Text: "Practice reply: yes, let's choose a place and time.")
            })
            {
                if (!(await context.Set<ChatMessage>().AnyAsync(m => m.MatchId == match.Id && m.SenderId == message.Sender && m.Content == message.Text, cancellationToken)))
                {
                    context.Set<ChatMessage>().Add(new ChatMessage { MatchId = match.Id, SenderId = message.Sender, Content = message.Text, CreatedAt = now.AddDays(-29) });
                    await context.SaveChangesAsync(cancellationToken);
                }
            }
        }

        var passer = users[2].Id;
        var passed = users[4].Id;
        if (!(await context.Set<Swipe>().AnyAsync(s => s.SwiperId == passer && s.TargetId == passed, cancellationToken)))
        {
            context.Set<Swipe>().Add(new Swipe { SwiperId = passer, TargetId = passed, Direction = "pass", CreatedAt = now.AddDays(-2) });
            await context.SaveChangesAsync(cancellationToken);
        }

        // One unfinished fight per match; history can contain completed/canceled fights.
        var sessions = new[]
        {
            (Match: 0, Status: "completed", Days: -14, Venue: "Seed QC history court", Address: "Fictional test venue, Quezon City", Confirmations: 2),
            (Match: 0, Status: "confirmed", Days: 7, Venue: "Seed QC weekend court", Address: "Fictional test venue, Quezon City", Confirmations: 2),
            (Match: 1, Status: "scheduled", Days: 5, Venue: "Seed Pasig technique court", Address: "Fictional test venue, Pasig", Confirmations: 1),
            (Match: 2, Status: "completed", Days: -20, Venue: "Seed Rizal practice court", Address: "Fictional test venue, Rizal", Confirmations: 2),
            (Match: 3, Status: "canceled", Days: -7, Venue: "Seed Makati canceled court", Address: "Fictional test venue, Makati", Confirmations: 0),
            (Match: 4, Status: "confirmed", Days: -1, Venue: "Seed Manila completion court", Address: "Fictional test venue, Manila", Confirmations: 2)
        };
        foreach (var session in sessions)
        {
            var match = matches[session.Match];
            var fight = await context.Set<Fight>().FirstOrDefaultAsync(f => f.MatchId == match.Id && f.Location == session.Venue, cancellationToken);
            if (fight == null)
            {
                fight = new Fight
                {
                    MatchId = match.Id,
                    ScheduledAt = now.AddDays(session.Days),
                    Location = session.Venue,
                    Address = session.Address,
                    Status = session.Status,
                    CancellationReason = session.Status == "canceled" ? "Practice cancellation: venue unavailable." : null,
                    CreatedAt = now.AddDays(-28),
                    UpdatedAt = now.AddDays(-1)
                };
                context.Set<Fight>().Add(fight);
                await context.SaveChangesAsync(cancellationToken);
            }
            var participants = new[] { match.User1Id, match.User2Id };
            for (var index = 0; index < session.Confirmations; index++)
            {
                var participant = participants[index];
                if (!(await context.Set<FightConfirmation>().AnyAsync(c => c.FightId == fight.Id && c.UserId == participant, cancellationToken)))
                {
                    context.Set<FightConfirmation>().Add(new FightConfirmation
                    {
                        FightId = fight.Id,
                        UserId = participant,
                        EmergencyContactName = "Fictional test contact",
                        EmergencyContactPhone = "000-000-0000",
                        AcceptedAt = fight.ScheduledAt < now ? fight.ScheduledAt.AddDays(-1) : now.AddDays(-1)
                    });
                    await context.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }));

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

builder.Services.AddAuthorization();

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

// Prepare the local database and run the inline EF seeds before accepting requests.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
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
