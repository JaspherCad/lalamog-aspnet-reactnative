namespace Lalamog.Api.Models;

public class SystemFeature
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public List<RoleSystemFeature> RoleSystemFeatures { get; set; } = [];
}
