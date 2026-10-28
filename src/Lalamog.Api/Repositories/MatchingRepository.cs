using Lalamog.Api.Data;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Lalamog.Api.Repositories;


public class MatchingRepository(AppDbContext db) : IMatchingRepository
{
    // Serializable isolation protects reciprocal likes across API instances.
    // SQL Server's retry strategy reruns the whole operation after a deadlock.
    public async Task<T> InTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct) =>
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var result = await action();
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        });

    public Task<Swipe?> SwipeAsync(Guid from, Guid to, CancellationToken ct) =>
        db.Swipes.SingleOrDefaultAsync(x => x.SwiperId == from && x.TargetId == to, ct);
    public Task<FighterMatch?> PairAsync(Guid first, Guid second, CancellationToken ct) =>
        db.Matches.SingleOrDefaultAsync(x => x.User1Id == first && x.User2Id == second, ct);
    public Task<FighterMatch?> GetAsync(long id, CancellationToken ct) =>
        db.Matches.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<List<FighterMatch>> ListAsync(Guid userId, CancellationToken ct) =>
        db.Matches.AsNoTracking().Where(x => x.User1Id == userId || x.User2Id == userId)
            .OrderByDescending(x => x.Id).Take(100).ToListAsync(ct);
    public void Add(Swipe swipe) => db.Swipes.Add(swipe);
    public void Add(FighterMatch match) => db.Matches.Add(match);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
