namespace Lalamog.Api.DTOs
{
    public class ConfirmationResponse
    {
        public Guid UserId { get; set; }

        public string EmergencyContactName { get; set; } = string.Empty;

        public string EmergencyContactPhone { get; set; } = string.Empty;

        public DateTimeOffset AcceptedAt { get; set; }
    }
}
