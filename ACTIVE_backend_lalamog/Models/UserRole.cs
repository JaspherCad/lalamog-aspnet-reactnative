namespace Lalamog.Api.Models;

public class UserRole
{
    public Guid UserId { get; set; }
    public int RoleId { get; set; }
    public AppUser User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
