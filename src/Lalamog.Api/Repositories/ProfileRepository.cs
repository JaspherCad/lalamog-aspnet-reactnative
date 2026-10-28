using Lalamog.Api.Data;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Microsoft.EntityFrameworkCore;
namespace Lalamog.Api.Repositories;
public class ProfileRepository(AppDbContext db) : IProfileRepository
{
    public Task<FighterProfile?> GetAsync(Guid userId, CancellationToken ct) =>
        db.Profiles.SingleOrDefaultAsync(x => x.UserId == userId, ct);
    public Task<List<FighterProfile>> DiscoverAsync(Guid userId, CancellationToken ct) =>
        db.Profiles.AsNoTracking().Where(p => p.UserId != userId
            && !db.Swipes.Any(s => s.SwiperId == userId && s.TargetId == p.UserId)
            && !db.Matches.Any(m => (m.User1Id == userId && m.User2Id == p.UserId)
                || (m.User2Id == userId && m.User1Id == p.UserId)))
            .OrderBy(p => p.Nickname).ThenBy(p => p.UserId).Take(50).ToListAsync(ct);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}