namespace Lalamog.Api.Models;

public class Swipe
{
    public long Id { get; set; }
    public Guid SwiperId { get; set; }
    public Guid TargetId { get; set; }
    public string Direction { get; set; } = "pass";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
