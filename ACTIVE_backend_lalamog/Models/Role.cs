namespace Lalamog.Api.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<UserRole> UserRoles { get; set; } = [];
    public List<RoleSystemFeature> RoleSystemFeatures { get; set; } = [];
}
