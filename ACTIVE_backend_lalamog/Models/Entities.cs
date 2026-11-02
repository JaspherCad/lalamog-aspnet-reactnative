using Microsoft.AspNetCore.Identity;
namespace Lalamog.Api.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public FighterProfile Profile { get; set; } = null!;
}
public class FighterProfile
{
    public FighterProfile()
    {
        FighterName = "fighter-" + Guid.NewGuid().ToString("N");
        NormalizedFighterName = FighterName.ToUpperInvariant();
    }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string Nickname { get; set; } = "";
    public string FighterName { get; set; } = "";
    public string NormalizedFighterName { get; set; } = "";
    public string Bio { get; set; } = "";
    public string FightingStyle { get; set; } = "Boxing";
    public int ExperienceLevel { get; set; } = 1;
    public string Location { get; set; } = "";
    public string Availability { get; set; } = "";
    public string? AvatarKey { get; set; }
}
public class Swipe
{
    public long Id { get; set; }
    public Guid SwiperId { get; set; }
    public Guid TargetId { get; set; }
    public string Direction { get; set; } = "pass";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
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
public class ChatMessage
{
    public long Id { get; set; }
    public long MatchId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public class Fight
{
    public long Id { get; set; }
    public long MatchId { get; set; }
    public FighterMatch Match { get; set; } = null!;
    public DateTimeOffset ScheduledAt { get; set; }
    public string Location { get; set; } = "";
    public string Address { get; set; } = "";
    public string Status { get; set; } = "scheduled";
    public string? CancellationReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public byte[] Version { get; set; } = [];
    public List<FightConfirmation> Confirmations { get; set; } = [];
}
public class FightConfirmation
{
    public long FightId { get; set; }
    public Fight Fight { get; set; } = null!;
    public Guid UserId { get; set; }
    public string EmergencyContactName { get; set; } = "";
    public string EmergencyContactPhone { get; set; } = "";
    public DateTimeOffset AcceptedAt { get; set; }
}
