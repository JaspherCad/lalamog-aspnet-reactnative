using Lalamog.Api.Authorization;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lalamog.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        // Create the feature catalog and default grants only when their role is new.
        var featureDefinitions = new[]
        {
            (Code: ApplicationPermissions.User_View, Description: "View your profile and discover public fighter profiles."),
            (Code: ApplicationPermissions.User_EditProfile, Description: "Edit your own fighter profile."),
            (Code: ApplicationPermissions.User_Upload, Description: "Upload your own avatar image."),
            (Code: ApplicationPermissions.User_Match, Description: "Like or pass fighters and list your mutual matches."),
            (Code: ApplicationPermissions.User_Chat, Description: "Read and send messages in your own matches."),
            (Code: ApplicationPermissions.User_Schedule, Description: "View and manage sessions in your own matches."),
            (Code: ApplicationPermissions.User_Map, Description: "Request an address suggestion for an explicit map pin."),
            (Code: ApplicationPermissions.Admin_ManageAccess, Description: "View system features and manage role/feature assignments.")
        };
        foreach (var definition in featureDefinitions)
        {
            if (!(await (from feature in context.SystemFeatures
                  where feature.Code == definition.Code select feature).AnyAsync(cancellationToken)))
            {
                context.Set<SystemFeature>().Add(new SystemFeature { Code = definition.Code, Description = definition.Description });
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        var userRole = await (from role in context.Roles
                              where role.Name == "User" select role).SingleOrDefaultAsync(cancellationToken);
        if (userRole == null)
        {
            userRole = new Role { Name = "User", Description = "Standard app features for a fighter." };
            context.Set<Role>().Add(userRole);
            await context.SaveChangesAsync(cancellationToken);
            foreach (var definition in (from item in featureDefinitions
                                        where item.Code.StartsWith("user_") select item))
            {
                var feature = await (from stored in context.SystemFeatures
                                     where stored.Code == definition.Code select stored).SingleAsync(cancellationToken);
                context.Set<RoleSystemFeature>().Add(new RoleSystemFeature { RoleId = userRole.Id, SystemFeatureId = feature.Id });
            }
            // One-time upgrade: retain access for existing accounts, without regranting on restarts.
            foreach (var existingUser in await context.Set<AppUser>().ToListAsync(cancellationToken))
            {
                context.Set<UserRole>().Add(new UserRole { UserId = existingUser.Id, RoleId = userRole.Id });
            }
            await context.SaveChangesAsync(cancellationToken);
        }
        var adminRole = await (from role in context.Roles
                               where role.Name == "Admin" select role).SingleOrDefaultAsync(cancellationToken);
        bool initializeAdminRole = adminRole == null;
        if (adminRole == null)
        {
            adminRole = new Role { Name = "Admin", Description = "Manages role and system-feature assignments." };
            context.Set<Role>().Add(adminRole);
            await context.SaveChangesAsync(cancellationToken);
            var feature = await (from stored in context.SystemFeatures
                                 where stored.Code == ApplicationPermissions.Admin_ManageAccess select stored).SingleAsync(cancellationToken);
            context.Set<RoleSystemFeature>().Add(new RoleSystemFeature { RoleId = adminRole.Id, SystemFeatureId = feature.Id });
            await context.SaveChangesAsync(cancellationToken);
        }

        var people = new[]
        {
            (Email: "alex.seed@example.test", Name: "Seed-Alex", Alias: "Iron", Style: "kickboxing", Level: 2, City: "Quezon City", Availability: "Saturday mornings"),
            (Email: "sam.seed@example.test", Name: "Seed-Sam", Alias: "Flow", Style: "bjj", Level: 2, City: "Manila", Availability: "Weekends"),
            (Email: "jordan.seed@example.test", Name: "Seed-Jordan", Alias: "Storm", Style: "wrestling", Level: 3, City: "Pasig", Availability: "Weekday evenings"),
            (Email: "miko.seed@example.test", Name: "Seed-Miko", Alias: "Crow", Style: "streetfighting|kickboxing", Level: 1, City: "Rizal", Availability: "Sunday afternoons"),
            (Email: "jae.seed@example.test", Name: "Seed-Jae", Alias: "Reaper", Style: "martial", Level: 2, City: "Makati", Availability: "Friday evenings"),
            (Email: "ana.seed@example.test", Name: "Seed-Ana", Alias: "Blaze", Style: "kickboxing|bjj", Level: 1, City: "Marikina", Availability: "Weekends"),
            // Discovery-only accounts: keep them out of the swipes and pairs below.
            (Email: "nico.seed@example.test", Name: "Seed-Nico", Alias: "Rook", Style: "kickboxing", Level: 1, City: "Quezon City", Availability: "Weekday evenings"),
            (Email: "bea.seed@example.test", Name: "Seed-Bea", Alias: "Viper", Style: "bjj", Level: 2, City: "Manila", Availability: "Saturday mornings"),
            (Email: "carlo.seed@example.test", Name: "Seed-Carlo", Alias: "Atlas", Style: "wrestling", Level: 3, City: "Pasig", Availability: "Sunday afternoons"),
            (Email: "dani.seed@example.test", Name: "Seed-Dani", Alias: "Comet", Style: "martial", Level: 1, City: "Makati", Availability: "Weekends"),
            (Email: "enzo.seed@example.test", Name: "Seed-Enzo", Alias: "Bolt", Style: "streetfighting|kickboxing", Level: 2, City: "Marikina", Availability: "Friday evenings"),
            (Email: "faye.seed@example.test", Name: "Seed-Faye", Alias: "Phoenix", Style: "kickboxing|bjj", Level: 2, City: "Taguig", Availability: "Weekday mornings"),
            (Email: "gio.seed@example.test", Name: "Seed-Gio", Alias: "Anchor", Style: "wrestling|bjj", Level: 3, City: "Mandaluyong", Availability: "Saturday afternoons"),
            (Email: "hana.seed@example.test", Name: "Seed-Hana", Alias: "Echo", Style: "martial|kickboxing", Level: 1, City: "San Juan", Availability: "Tuesday and Thursday evenings"),
            (Email: "ivan.seed@example.test", Name: "Seed-Ivan", Alias: "Talon", Style: "streetfighting|wrestling", Level: 2, City: "Pasay", Availability: "Sunday mornings"),
            (Email: "luna.seed@example.test", Name: "Seed-Luna", Alias: "Nova", Style: "martial|bjj", Level: 1, City: "Antipolo", Availability: "Weekends"),
            (Email: "omar.seed@example.test", Name: "Seed-Omar", Alias: "Flint", Style: "kickboxing|wrestling", Level: 2, City: "Valenzuela", Availability: "Wednesday evenings"),
            (Email: "pia.seed@example.test", Name: "Seed-Pia", Alias: "Sable", Style: "bjj|martial", Level: 1, City: "Las Pinas", Availability: "Saturday mornings"),
            (Email: "rafa.seed@example.test", Name: "Seed-Rafa", Alias: "Hawk", Style: "streetfighting", Level: 3, City: "Paranaque", Availability: "Sunday afternoons"),
            (Email: "tala.seed@example.test", Name: "Seed-Tala", Alias: "Ember", Style: "kickboxing|martial", Level: 1, City: "Muntinlupa", Availability: "Weekends"),
            (Email: "yuri.seed@example.test", Name: "Seed-Yuri", Alias: "Drift", Style: "wrestling|bjj", Level: 2, City: "Caloocan", Availability: "Friday evenings")
        };
        var users = new List<AppUser>();
        var now = DateTimeOffset.UtcNow;

        // Save each parent first, then create its profile using the saved ID.
        foreach (var person in people)
        {
            var email = person.Email.ToUpperInvariant();
            var user = await (from account in context.Users
                              where account.NormalizedEmail == email select account).FirstOrDefaultAsync(cancellationToken);
            bool newUser = user == null;
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
            if (newUser)
            {
                context.Set<UserRole>().Add(new UserRole { UserId = user.Id, RoleId = userRole.Id });
                await context.SaveChangesAsync(cancellationToken);
            }
            // Only the fictional Alex account is a demo administrator.
            if (person.Email == "alex.seed@example.test" && (newUser || initializeAdminRole))
            {
                context.Set<UserRole>().Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });
                await context.SaveChangesAsync(cancellationToken);
            }
            users.Add(user);

            var profile = await (from stored in context.Profiles
                                 where stored.UserId == user.Id select stored).SingleOrDefaultAsync(cancellationToken);
            if (profile == null)
            {
                profile = new FighterProfile
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
                };
                context.Set<FighterProfile>().Add(profile);
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        // Only the original first five participate; Ana and all fifteen additions have no seeded swipes/matches.
        var pairs = new[] { (Left: 0, Right: 1), (Left: 0, Right: 2), (Left: 0, Right: 3), (Left: 0, Right: 4), (Left: 1, Right: 2) };
        var matches = new List<FighterMatch>();
        foreach (var pair in pairs)
        {
            var left = users[pair.Left].Id;
            var right = users[pair.Right].Id;
            foreach (var swipe in new[] { (From: left, To: right), (From: right, To: left) })
            {
                if (!(await (from stored in context.Swipes
                              where stored.SwiperId == swipe.From && stored.TargetId == swipe.To
                              select stored).AnyAsync(cancellationToken)))
                {
                    context.Set<Swipe>().Add(new Swipe { SwiperId = swipe.From, TargetId = swipe.To, Direction = "like", CreatedAt = now.AddDays(-30) });
                    await context.SaveChangesAsync(cancellationToken);
                }
            }
            if (string.CompareOrdinal(left.ToString("D"), right.ToString("D")) > 0)
            {
                (left, right) = (right, left);
            }
            var match = await (from stored in context.Matches
                               where stored.User1Id == left && stored.User2Id == right
                               select stored).FirstOrDefaultAsync(cancellationToken);
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
                if (!(await (from stored in context.Messages
                              where stored.MatchId == match.Id && stored.SenderId == message.Sender && stored.Content == message.Text
                              select stored).AnyAsync(cancellationToken)))
                {
                    context.Set<ChatMessage>().Add(new ChatMessage { MatchId = match.Id, SenderId = message.Sender, Content = message.Text, CreatedAt = now.AddDays(-29) });
                    await context.SaveChangesAsync(cancellationToken);
                }
            }
        }

        var passer = users[2].Id;
        var passed = users[4].Id;
        if (!(await (from swipe in context.Swipes
                      where swipe.SwiperId == passer && swipe.TargetId == passed select swipe).AnyAsync(cancellationToken)))
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
            var fight = await (from stored in context.Fights
                               where stored.MatchId == match.Id && stored.Location == session.Venue
                               select stored).FirstOrDefaultAsync(cancellationToken);
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
                if (!(await (from confirmation in context.FightConfirmations
                              where confirmation.FightId == fight.Id && confirmation.UserId == participant
                              select confirmation).AnyAsync(cancellationToken)))
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
    }
}
