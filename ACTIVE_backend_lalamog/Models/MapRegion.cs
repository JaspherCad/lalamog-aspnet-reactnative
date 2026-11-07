namespace Lalamog.Api.Models;

public class MapRegion
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public List<MapProvince> Provinces { get; set; } = [];
    public List<MapCity> IndependentCities { get; set; } = [];
}
