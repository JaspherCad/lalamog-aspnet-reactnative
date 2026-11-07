namespace Lalamog.Api.DTOs;
public class GymBookingResponse
{
    public long Id { get; set; }
    public long GymId { get; set; }
    public string GymName { get; set; } = "";
    public string Fighter1Name { get; set; } = "";
    public string Fighter2Name { get; set; } = "";
    public DateTimeOffset ScheduledAt { get; set; }
    public decimal? QuotedPrice { get; set; }
    public int? SessionDurationMinutes { get; set; }
    public string Status { get; set; } = "";
    public int ConfirmationCount { get; set; }
    public string? CancellationReason { get; set; }
}
