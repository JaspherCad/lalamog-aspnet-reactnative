namespace Lalamog.Api.Models;

public class MapCity
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int? ProvinceId { get; set; }
    public MapProvince? Province { get; set; }
    // Independent cities belong directly to a region; provincial cities use ProvinceId.
    public int? RegionId { get; set; }
    public MapRegion? Region { get; set; }
    public List<MapBarangay> Barangays { get; set; } = [];
    public List<Gym> Gyms { get; set; } = [];
}
