namespace Lalamog.Api.DTOs
{
    public class FightResponse
    {
        public long Id { get; set; }

        public long MatchId { get; set; }

        public Guid User1Id { get; set; }

        public Guid User2Id { get; set; }

        public DateTimeOffset ScheduledAt { get; set; }

        public string Location { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public bool User1Confirmed { get; set; }

        public bool User2Confirmed { get; set; }

        public string? CancellationReason { get; set; }

        public List<ConfirmationResponse> Confirmations { get; set; } = new List<ConfirmationResponse>();
        public MapLocation? MapLocation { get; set; }
    }
}
