using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;

namespace Lalamog.Api.Services
{
    public class MatchingService : IMatchingService
    {
        private readonly IMatchingRepository _matchingRepository;
        private readonly IProfileRepository _profileRepository;

        public MatchingService(IMatchingRepository matchingRepository, IProfileRepository profileRepository)
        {
            _matchingRepository = matchingRepository;
            _profileRepository = profileRepository;
        }

        public async Task<SwipeResponse> SwipeAsync(Guid actor, SwipeRequest request, CancellationToken cancellationToken)
        {
            if (actor == request.TargetId)
            {
                throw new ApiException(400, "You cannot swipe on yourself.");
            }
            if (request.Direction != "like" && request.Direction != "pass")
            {
                throw new ApiException(400, "Choose like or pass.");
            }

            async Task<SwipeResponse> ApplySwipeAsync()
            {
                FighterProfile? target = await _profileRepository.GetAsync(request.TargetId, cancellationToken);
                if (target == null)
                {
                    throw new ApiException(404, "Fighter not found.");
                }

                (Guid first, Guid second) = CanonicalPair(actor, request.TargetId);
                // Read both directions in the same order for every caller and retry.
                Swipe? forward = await _matchingRepository.SwipeAsync(first, second, cancellationToken);
                Swipe? reverse = await _matchingRepository.SwipeAsync(second, first, cancellationToken);
                Swipe? own;
                Swipe? other;
                if (actor == first)
                {
                    own = forward;
                    other = reverse;
                }
                else
                {
                    own = reverse;
                    other = forward;
                }

                if (own == null)
                {
                    own = new Swipe { SwiperId = actor, TargetId = request.TargetId };
                    _matchingRepository.Add(own);
                }
                own.Direction = request.Direction;
                own.CreatedAt = DateTimeOffset.UtcNow;

                FighterMatch? match = await _matchingRepository.PairAsync(first, second, cancellationToken);
                if (match == null && own.Direction == "like" && other?.Direction == "like")
                {
                    match = new FighterMatch { User1Id = first, User2Id = second };
                    _matchingRepository.Add(match);
                }

                await _matchingRepository.SaveAsync(cancellationToken);
                return new SwipeResponse { Matched = match != null, MatchId = match?.Id };
            }

            return await _matchingRepository.InTransactionAsync(ApplySwipeAsync, cancellationToken);
        }

        public static (Guid First, Guid Second) CanonicalPair(Guid firstUser, Guid secondUser)
        {
            int comparison = string.CompareOrdinal(firstUser.ToString("D"), secondUser.ToString("D"));
            if (comparison < 0)
            {
                return (firstUser, secondUser);
            }
            return (secondUser, firstUser);
        }

        public async Task<FighterMatch> RequireMemberAsync(long matchId, Guid actor, CancellationToken cancellationToken)
        {
            FighterMatch? match = await _matchingRepository.GetAsync(matchId, cancellationToken);
            if (match == null || !match.HasMember(actor))
            {
                throw new ApiException(404, "Match not found.");
            }
            return match;
        }

        public async Task<List<MatchResponse>> ListAsync(Guid actor, CancellationToken cancellationToken)
        {
            List<FighterMatch> matches = await _matchingRepository.ListAsync(actor, cancellationToken);
            var responses = new List<MatchResponse>();
            foreach (FighterMatch match in matches)
            {
                Guid opponentId;
                if (match.User1Id == actor)
                {
                    opponentId = match.User2Id;
                }
                else
                {
                    opponentId = match.User1Id;
                }

                FighterProfile? opponent = await _profileRepository.GetAsync(opponentId, cancellationToken);
                if (opponent != null)
                {
                    responses.Add(new MatchResponse
                    {
                        Id = match.Id,
                        Opponent = DtoMapper.ToProfileResponse(opponent),
                        CreatedAt = match.CreatedAt
                    });
                }
            }
            return responses;
        }
    }
}
