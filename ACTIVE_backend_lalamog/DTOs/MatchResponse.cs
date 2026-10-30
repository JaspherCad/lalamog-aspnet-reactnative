namespace Lalamog.Api.DTOs
{
    public class MatchResponse
    {
        public long Id { get; set; }

        public ProfileResponse Opponent { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; }
    }
}
