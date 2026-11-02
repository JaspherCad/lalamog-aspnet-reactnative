namespace Lalamog.Api.Models;

public class FighterMatch
{
    public long Id { get; set; }
    public Guid User1Id { get; set; }
    public Guid User2Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool HasMember(Guid userId)
    {
        return User1Id == userId || User2Id == userId;
    }
}
