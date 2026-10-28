using Lalamog.Api.Data;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Microsoft.EntityFrameworkCore;
namespace Lalamog.Api.Repositories;
public class FightRepository(AppDbContext db) : IFightRepository
{
    public Task<Fight?> GetAsync(long id, CancellationToken ct) =>
        db.Fights.Include(x => x.Match).Include(x => x.Confirmations).SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<List<Fight>> ListAsync(Guid userId, CancellationToken ct) =>
        db.Fights.AsNoTracking().Include(x => x.Match).Include(x => x.Confirmations)
            .Where(x => x.Match.User1Id == userId || x.Match.User2Id == userId)
            .OrderByDescending(x => x.Id).Take(100).ToListAsync(ct);
    public Task<bool> HasUpcomingAsync(long matchId, CancellationToken ct) =>
        db.Fights.AnyAsync(x => x.MatchId == matchId && (x.Status == "scheduled" || x.Status == "confirmed"), ct);
    public void Add(Fight fight) => db.Fights.Add(fight);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}