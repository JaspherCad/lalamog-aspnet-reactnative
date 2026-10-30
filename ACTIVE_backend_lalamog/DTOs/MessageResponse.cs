namespace Lalamog.Api.DTOs
{
    public class MessageResponse
    {
        public long Id { get; set; }

        public long MatchId { get; set; }

        public Guid SenderId { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
    }
}
