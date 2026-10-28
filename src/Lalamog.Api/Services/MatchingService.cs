using Lalamog.Api.Contracts;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Models;
using Lalamog.Api.Interfaces;

namespace Lalamog.Api.Services;

public class MatchingService(IMatchingRepository matches, IProfileRepository profiles)
{
    public async Task<SwipeResponse> SwipeAsync(Guid actor, SwipeRequest request, CancellationToken ct)
    {
        if (actor == request.TargetId) throw new ApiException(400, "You cannot swipe on yourself.");
        if (request.Direction is not ("like" or "pass")) throw new ApiException(400, "Choose like or pass.");
        return await matches.InTransactionAsync(async () =>
        {
            if (await profiles.GetAsync(request.TargetId, ct) is null)
                throw new ApiException(404, "Fighter not found.");
            var (first, second) = CanonicalPair(actor, request.TargetId);
            // Read both directions in the same order for every caller.
            var forward = await matches.SwipeAsync(first, second, ct);
            var reverse = await matches.SwipeAsync(second, first, ct);
            var own = actor == first ? forward : reverse;
            var other = actor == first ? reverse : forward;
            if (own is null)
            {
                own = new Swipe { SwiperId = actor, TargetId = request.TargetId };
                matches.Add(own);
            }
            own.Direction = request.Direction;
            own.CreatedAt = DateTimeOffset.UtcNow;
            var match = await matches.PairAsync(first, second, ct);
            if (match is null && own.Direction == "like" && other?.Direction == "like")
            {
                match = new FighterMatch { User1Id = first, User2Id = second };
                matches.Add(match);
            }
            await matches.SaveAsync(ct);
            return new SwipeResponse(match is not null, match?.Id);
        }, ct);
    }

    public static (Guid First, Guid Second) CanonicalPair(Guid a, Guid b) =>
        string.CompareOrdinal(a.ToString("D"), b.ToString("D")) < 0 ? (a, b) : (b, a);

    public async Task<FighterMatch> RequireMemberAsync(long matchId, Guid actor, CancellationToken ct)
    {
        var match = await matches.GetAsync(matchId, ct);
        if (match is null || !match.HasMember(actor)) throw new ApiException(404, "Match not found.");
        return match;
    }

    public async Task<List<MatchResponse>> ListAsync(Guid actor, CancellationToken ct)
    {
        var result = new List<MatchResponse>();
        foreach (var match in await matches.ListAsync(actor, ct))
        {
            var opponentId = match.User1Id == actor ? match.User2Id : match.User1Id;
            var opponent = await profiles.GetAsync(opponentId, ct);
            if (opponent is not null) result.Add(new(match.Id, ProfileService.ToResponse(opponent), match.CreatedAt));
        }
        return result;
    }
}
