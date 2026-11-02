namespace Lalamog.Api.Models;

public class Fight
{
    public long Id { get; set; }
    public long MatchId { get; set; }
    public FighterMatch Match { get; set; } = null!;
    public DateTimeOffset ScheduledAt { get; set; }
    public string Location { get; set; } = "";
    public string Address { get; set; } = "";
    public string Status { get; set; } = "scheduled";
    public string? CancellationReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public byte[] Version { get; set; } = [];
    public List<FightConfirmation> Confirmations { get; set; } = [];
    public string? RegionCode { get; set; }
    public string? ProvinceCode { get; set; }
    public string? CityCode { get; set; }
    public string? BarangayCode { get; set; }
    public string? PsgcVersion { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
