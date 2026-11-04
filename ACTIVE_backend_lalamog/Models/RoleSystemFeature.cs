namespace Lalamog.Api.Models;

public class RoleSystemFeature
{
    public int RoleId { get; set; }
    public int SystemFeatureId { get; set; }
    public Role Role { get; set; } = null!;
    public SystemFeature SystemFeature { get; set; } = null!;
}
