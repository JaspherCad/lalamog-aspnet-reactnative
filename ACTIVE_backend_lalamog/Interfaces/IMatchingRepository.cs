using Lalamog.Api.Models;

namespace Lalamog.Api.Interfaces
{
    public interface IMatchingRepository
    {
        Task<T> InTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken);

        Task<Swipe?> SwipeAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken);

        Task<FighterMatch?> PairAsync(Guid first, Guid second, CancellationToken cancellationToken);

        Task<FighterMatch?> GetAsync(long id, CancellationToken cancellationToken);

        Task<List<FighterMatch>> ListAsync(Guid userId, CancellationToken cancellationToken);

        void Add(Swipe swipe);

        void Add(FighterMatch match);

        Task SaveAsync(CancellationToken cancellationToken);
    }
}
