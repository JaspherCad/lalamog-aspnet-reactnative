namespace Lalamog.Api.DTOs;

public class MapLocation
{
    public string? RegionCode { get; set; }
    public string? ProvinceCode { get; set; }
    public string? CityCode { get; set; }
    public string? BarangayCode { get; set; }
    public string? PsgcVersion { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
