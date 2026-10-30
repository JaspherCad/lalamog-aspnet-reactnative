using Lalamog.Api.Data;
using Lalamog.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lalamog.Api.Infrastructure
{
    public class DatabaseInitializer
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public DatabaseInitializer(AppDbContext db, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _db = db;
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await _db.Database.MigrateAsync(cancellationToken);
            if (!_configuration.GetValue<bool>("Seed:Demo"))
            {
                return;
            }

            string? password = _configuration["Seed:Password"];
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Set Seed__Password when demo seeding is enabled.");
            }

            var accounts = new[]
            {
                (Email: "alex@example.test", Nickname: "Alex", Style: "Boxing"),
                (Email: "sam@example.test", Nickname: "Sam", Style: "Muay Thai"),
                (Email: "jordan@example.test", Nickname: "Jordan", Style: "MMA")
            };
            foreach (var account in accounts)
            {
                if (await _userManager.FindByEmailAsync(account.Email) != null)
                {
                    continue;
                }

                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    Email = account.Email,
                    UserName = account.Email,
                    Profile = new FighterProfile
                    {
                        Nickname = account.Nickname,
                        FightingStyle = account.Style,
                        Location = "Metro Manila",
                        Bio = "Looking for friendly technical sparring.",
                        Availability = "Weekends"
                    }
                };

                IdentityResult result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    var errors = new List<string>();
                    foreach (IdentityError error in result.Errors)
                    {
                        errors.Add(error.Description);
                    }
                    throw new InvalidOperationException(string.Join(" ", errors));
                }
            }
        }
    }
}
