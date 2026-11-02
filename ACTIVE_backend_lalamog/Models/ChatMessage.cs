namespace Lalamog.Api.Models;

public class ChatMessage
{
    public long Id { get; set; }
    public long MatchId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
