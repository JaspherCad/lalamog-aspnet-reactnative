using Lalamog.Api.DTOs;
using Lalamog.Api.Models;

namespace Lalamog.Api.Interfaces
{
    public interface IMatchingService
    {
        Task<SwipeResponse> SwipeAsync(Guid actor, SwipeRequest request, CancellationToken cancellationToken);

        Task<List<MatchResponse>> ListAsync(Guid actor, CancellationToken cancellationToken);

        Task<FighterMatch> RequireMemberAsync(long matchId, Guid actor, CancellationToken cancellationToken);
    }
}
