using MyApi.DTOs;

namespace MyApi.Interfaces
{
    public interface IFightStatsService
    {
        Task<bool> RecordFightResultAsync(FightResultDto resultDto, Guid recordedByUserId);
        Task<UserStatsDto?> GetUserStatsAsync(Guid userId);
        Task<HeadToHeadStatsDto?> GetHeadToHeadStatsAsync(Guid userId1, Guid userId2, Guid requestingUserId);
        Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int page = 1, int pageSize = 50, string? fightingStyle = null, int? experienceLevel = null);
        Task<List<FightHistoryDto>> GetUserFightHistoryAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<bool> ValidateFightResultAsync(long fightScheduleId, Guid userId);
    }
}
