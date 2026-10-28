using Lalamog.Api.Models;
namespace Lalamog.Api.Interfaces;

public interface IProfileRepository
{
    Task<FighterProfile?> GetAsync(Guid userId, CancellationToken ct);
    Task<List<FighterProfile>> DiscoverAsync(Guid userId, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}
public interface IMatchingRepository
{
    Task<T> InTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct);
    Task<Swipe?> SwipeAsync(Guid from, Guid to, CancellationToken ct);
    Task<FighterMatch?> PairAsync(Guid first, Guid second, CancellationToken ct);
    Task<FighterMatch?> GetAsync(long id, CancellationToken ct);
    Task<List<FighterMatch>> ListAsync(Guid userId, CancellationToken ct);
    void Add(Swipe swipe);
    void Add(FighterMatch match);
    Task SaveAsync(CancellationToken ct);
}
public interface IConversationRepository
{
    Task<List<ChatMessage>> ListAsync(long matchId, long? before, long? after, CancellationToken ct);
    Task AddAsync(ChatMessage message, CancellationToken ct);
}
public interface IFightRepository
{
    Task<Fight?> GetAsync(long id, CancellationToken ct);
    Task<List<Fight>> ListAsync(Guid userId, CancellationToken ct);
    Task<bool> HasUpcomingAsync(long matchId, CancellationToken ct);
    void Add(Fight fight);
    Task SaveAsync(CancellationToken ct);
}