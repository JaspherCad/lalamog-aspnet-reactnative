namespace Lalamog.Api.Models;

public class MapProvince
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int RegionId { get; set; }
    public MapRegion Region { get; set; } = null!;
    public List<MapCity> Cities { get; set; } = [];
}
