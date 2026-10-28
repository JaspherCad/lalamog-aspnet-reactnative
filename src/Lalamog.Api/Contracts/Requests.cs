using System.ComponentModel.DataAnnotations;
namespace Lalamog.Api.Contracts;
public record RegisterRequest(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, StringLength(128, MinimumLength = 10)] string Password,
    [Required, StringLength(50, MinimumLength = 2)] string Nickname);
public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public record ProfileRequest(
    [Required, StringLength(50, MinimumLength = 2)] string Nickname,
    [MaxLength(500)] string Bio,
    [Required, MaxLength(50)] string FightingStyle,
    [Range(1, 3)] int ExperienceLevel,
    [MaxLength(200)] string Location,
    [MaxLength(200)] string Availability);
public record SwipeRequest(Guid TargetId, [Required, RegularExpression("^(like|pass)$")] string Direction);
public record MessageRequest([Required, StringLength(2000, MinimumLength = 1)] string Content);
public record ScheduleRequest(long MatchId, DateTimeOffset ScheduledAt,
    [Required, MaxLength(200)] string Location, [Required, MaxLength(300)] string Address);
public record ConfirmRequest(bool Accepted,
    [Required, MaxLength(100)] string EmergencyContactName,
    [Required, MaxLength(40)] string EmergencyContactPhone);
public record CancelRequest([Required, MaxLength(500)] string Reason);
public record AuthResponse(Guid UserId);
public record ProfileResponse(Guid UserId, string Nickname, string Bio, string FightingStyle,
    int ExperienceLevel, string Location, string Availability, string? AvatarUrl);
public record MatchResponse(long Id, ProfileResponse Opponent, DateTimeOffset CreatedAt);
public record SwipeResponse(bool Matched, long? MatchId);
public record ConfirmationResponse(Guid UserId, string EmergencyContactName, string EmergencyContactPhone, DateTimeOffset AcceptedAt);
public record FightResponse(long Id, long MatchId, Guid User1Id, Guid User2Id,
    DateTimeOffset ScheduledAt, string Location, string Address, string Status,
    bool User1Confirmed, bool User2Confirmed, string? CancellationReason,
    List<ConfirmationResponse> Confirmations);