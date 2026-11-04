namespace Lalamog.Api.DTOs;

public class RoleResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<SystemFeatureResponse> SystemFeatures { get; set; } = [];
}
