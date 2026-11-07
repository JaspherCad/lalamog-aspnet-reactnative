namespace Lalamog.Api.Models;

public class FighterProfile
{
    public FighterProfile()
    {
        FighterName = "fighter-" + Guid.NewGuid().ToString("N");
        NormalizedFighterName = FighterName.ToUpperInvariant();
    }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public string Nickname { get; set; } = "";
    public string FighterName { get; set; } = "";
    public string NormalizedFighterName { get; set; } = "";
    public string Bio { get; set; } = "";
    public string FightingStyle { get; set; } = "Boxing";
    public int ExperienceLevel { get; set; } = 1;
    public string Location { get; set; } = "";
    public string Availability { get; set; } = "";
    public string? RegionCode { get; set; }
    public string? ProvinceCode { get; set; }
    public string? CityCode { get; set; }
    public string? BarangayCode { get; set; }
    public string? PsgcVersion { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
