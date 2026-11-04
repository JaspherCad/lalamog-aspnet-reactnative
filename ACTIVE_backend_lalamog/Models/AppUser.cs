namespace Lalamog.Api.Models;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public string? PasswordHash { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public byte[] Version { get; set; } = [];
    public List<UserRole> UserRoles { get; set; } = [];
    public FighterProfile Profile { get; set; } = null!;
}
