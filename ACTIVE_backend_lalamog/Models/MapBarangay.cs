namespace Lalamog.Api.Models;

public class MapBarangay
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int CityId { get; set; }
    public MapCity City { get; set; } = null!;
    public List<Gym> Gyms { get; set; } = [];
}
