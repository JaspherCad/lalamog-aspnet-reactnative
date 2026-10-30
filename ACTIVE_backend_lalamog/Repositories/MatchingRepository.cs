using System.Data;
using Lalamog.Api.Data;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lalamog.Api.Repositories
{
    public class MatchingRepository : IMatchingRepository
    {
        private readonly AppDbContext _db;

        public MatchingRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<T> InTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            async Task<T> ExecuteTransactionAsync()
            {
                // A retry must reload all state and repeat the entire operation.
                _db.ChangeTracker.Clear();
                await using var transaction = await _db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, cancellationToken);
                T result = await action();
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }

            return await strategy.ExecuteAsync(ExecuteTransactionAsync);
        }

        public Task<Swipe?> SwipeAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken)
        {
            var query = from swipe in _db.Swipes
                        where swipe.SwiperId == fromUserId && swipe.TargetId == toUserId
                        select swipe;
            return query.SingleOrDefaultAsync(cancellationToken);
        }

        public Task<FighterMatch?> PairAsync(Guid first, Guid second, CancellationToken cancellationToken)
        {
            var query = from match in _db.Matches
                        where match.User1Id == first && match.User2Id == second
                        select match;
            return query.SingleOrDefaultAsync(cancellationToken);
        }

        public Task<FighterMatch?> GetAsync(long id, CancellationToken cancellationToken)
        {
            var query = from match in _db.Matches
                        where match.Id == id
                        select match;
            return query.SingleOrDefaultAsync(cancellationToken);
        }

        public Task<List<FighterMatch>> ListAsync(Guid userId, CancellationToken cancellationToken)
        {
            var query = from match in _db.Matches.AsNoTracking()
                        where match.User1Id == userId || match.User2Id == userId
                        orderby match.Id descending
                        select match;
            return query.Take(100).ToListAsync(cancellationToken);
        }

        public void Add(Swipe swipe)
        {
            _db.Swipes.Add(swipe);
        }

        public void Add(FighterMatch match)
        {
            _db.Matches.Add(match);
        }

        public Task SaveAsync(CancellationToken cancellationToken)
        {
            return _db.SaveChangesAsync(cancellationToken);
        }
    }
}
