namespace Lalamog.Api.DTOs;

// Intentionally excludes participant IDs, waivers, contacts and cancellation text.
public class PublicFightResponse
{
    public string? Fighter1Name { get; set; }
    public string? Fighter2Name { get; set; }
    public string? Fighter1AvatarUrl { get; set; }
    public string? Fighter2AvatarUrl { get; set; }
    public long Id { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public string Status { get; set; } = "";
}
