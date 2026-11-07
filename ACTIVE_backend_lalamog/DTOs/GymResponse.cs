namespace Lalamog.Api.DTOs;

public class GymResponse
{
    public long Id { get; set; }
    public string Slug { get; set; } = "";
    public string Description { get; set; } = "";
    public string ContactInformation { get; set; } = "";
    public decimal SessionPrice { get; set; }
    public int SessionDurationMinutes { get; set; }
    public bool IsPublished { get; set; }
    public string Version { get; set; } = "";
    public string CoverUrl { get; set; } = "";
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsDemo { get; set; }
    public List<MapAreaResponse> AreaPath { get; set; } = [];
    public List<PublicFightResponse> Fights { get; set; } = [];
}
