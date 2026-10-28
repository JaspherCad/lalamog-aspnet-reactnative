using Lalamog.Api.Data;
using Lalamog.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Lalamog.Api.Infrastructure;

public class DatabaseInitializer(AppDbContext db, UserManager<ApplicationUser> users, IConfiguration config)
{
    public async Task InitializeAsync(CancellationToken ct)
    {
        await db.Database.MigrateAsync(ct);
        if (!config.GetValue<bool>("Seed:Demo")) return;
        var password = config["Seed:Password"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Set Seed__Password when demo seeding is enabled.");
        foreach (var (email, nickname, style) in new[]
        {
            ("alex@example.test", "Alex", "Boxing"),
            ("sam@example.test", "Sam", "Muay Thai"),
            ("jordan@example.test", "Jordan", "MMA")
        })
        {
            if (await users.FindByEmailAsync(email) is not null) continue;
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(), Email = email, UserName = email,
                Profile = new FighterProfile
                {
                    Nickname = nickname, FightingStyle = style, Location = "Metro Manila",
                    Bio = "Looking for friendly technical sparring.", Availability = "Weekends"
                }
            };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }
}