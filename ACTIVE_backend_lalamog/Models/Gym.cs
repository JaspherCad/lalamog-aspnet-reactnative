namespace Lalamog.Api.Models;

// A shared public venue, rather than a user's private location.
public class Gym
{
    public long Id { get; set; }
    public Guid? OwnerUserId { get; set; }
    public AppUser? Owner { get; set; }
    public string Description { get; set; } = "";
    public string ContactInformation { get; set; } = "";
    public decimal SessionPrice { get; set; }
    public int SessionDurationMinutes { get; set; } = 60;
    public bool IsPublished { get; set; }
    public byte[] Version { get; set; } = [];
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int CityId { get; set; }
    public MapCity City { get; set; } = null!;
    public int? BarangayId { get; set; }
    public MapBarangay? Barangay { get; set; }
    public bool IsDemo { get; set; }
    public List<Fight> Fights { get; set; } = [];
}
