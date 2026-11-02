namespace Lalamog.Api.DTOs;

public class ReverseGeocodeResponse
{
    public string Address { get; set; } = string.Empty;
    public string? Road { get; set; }
    public string? Suburb { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
}
