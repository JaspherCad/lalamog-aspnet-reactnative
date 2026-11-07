using System.Text.Json;

namespace Lalamog.Api.DTOs;

public class ReverseGeocodeResponse
{
    public string Address { get; set; } = string.Empty;
    public string? Road { get; set; }
    public string? Suburb { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string Provider { get; set; } = "Azure Maps";
    public string? StreetNumber { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
    public string? ResultType { get; set; }
    public string? Confidence { get; set; }
    public List<string> MatchCodes { get; set; } = [];
    // Complete Azure GeoJSON features retain all address parts, points and metadata.
    public List<JsonElement> Features { get; set; } = [];
}
