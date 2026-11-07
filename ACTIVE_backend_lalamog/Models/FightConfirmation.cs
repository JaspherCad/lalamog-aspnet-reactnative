namespace Lalamog.Api.Models;

public class FightConfirmation
{
    public long FightId { get; set; }
    public Fight Fight { get; set; } = null!;
    public Guid UserId { get; set; }
    public string EmergencyContactName { get; set; } = "";
    public string EmergencyContactPhone { get; set; } = "";
    public bool AllowPublicIdentity { get; set; }
    public DateTimeOffset AcceptedAt { get; set; }
}
