namespace Lalamog.Api.DTOs;
public class AccessUserResponse
{
    public bool CanManageGyms { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = "";
    public List<int> RoleIds { get; set; } = [];
}
