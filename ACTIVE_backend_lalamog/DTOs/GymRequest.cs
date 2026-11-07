using System.ComponentModel.DataAnnotations;

namespace Lalamog.Api.DTOs;

public class GymRequest
{
    [MaxLength(2000)]
    public string Description { get; set; } = "";
    [MaxLength(300)]
    public string ContactInformation { get; set; } = "";
    [Range(typeof(decimal), "0", "99999999.99")]
    public decimal SessionPrice { get; set; }
    [Range(30, 240)]
    public int SessionDurationMinutes { get; set; } = 60;
    public string? Version { get; set; }
    [Required, MaxLength(80), RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$")]
    public string Slug { get; set; } = "";
    [Required, MaxLength(200)]
    public string Name { get; set; } = "";
    [Required, MaxLength(300)]
    public string Address { get; set; } = "";
    [Required, MaxLength(10)]
    public string AreaCode { get; set; } = "";
    [Required, Range(-90, 90)]
    public double? Latitude { get; set; }
    [Required, Range(-180, 180)]
    public double? Longitude { get; set; }
}
