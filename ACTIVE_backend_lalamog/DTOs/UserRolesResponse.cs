namespace Lalamog.Api.DTOs;

public class UserRolesResponse
{
    public Guid UserId { get; set; }
    public List<int> RoleIds { get; set; } = [];
}
