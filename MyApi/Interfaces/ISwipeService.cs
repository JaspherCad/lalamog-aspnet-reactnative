using MyApi.DTOs;

namespace MyApi.Interfaces
{
    public interface ISwipeService
    {
        Task<SwipeResultDto> ProcessSwipeAsync(Guid swiperId, Guid swipeeId, string direction);
        // Task<IEnumerable<SwipeActionDto>> GetSwipeHistoryAsync(Guid userId);
        // Task<bool> CheckMutualSwipeAsync(Guid user1Id, Guid user2Id);

        Task<List<ProfileDto>> GetMatchedUserOfUserWithID(Guid userId);

        Task<List<ProfileDto>> FetchAllAvailableProfilesAsync(Guid userId);

        // Combined method to fetch both matched and available profiles in one call
        Task<SwipeProfilesDto> FetchAllProfilesAsync(Guid userId);

        Task<List<MatchDto>> GetAllMatchesForUser(Guid userId);
    }
}
