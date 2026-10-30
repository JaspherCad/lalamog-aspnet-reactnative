using Lalamog.Api.Data;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lalamog.Api.Repositories
{
    public class FightRepository : IFightRepository
    {
        private readonly AppDbContext _db;

        public FightRepository(AppDbContext db)
        {
            _db = db;
        }

        public Task<Fight?> GetAsync(long id, CancellationToken cancellationToken)
        {
            var query = from fight in _db.Fights
                            .Include(nameof(Fight.Match))
                            .Include(nameof(Fight.Confirmations))
                        where fight.Id == id
                        select fight;
            return query.SingleOrDefaultAsync(cancellationToken);
        }

        public Task<List<Fight>> ListAsync(Guid userId, CancellationToken cancellationToken)
        {
            var query = from fight in _db.Fights.AsNoTracking()
                            .Include(nameof(Fight.Match))
                            .Include(nameof(Fight.Confirmations))
                        where fight.Match.User1Id == userId || fight.Match.User2Id == userId
                        orderby fight.Id descending
                        select fight;
            return query.Take(100).ToListAsync(cancellationToken);
        }

        public Task<bool> HasUpcomingAsync(long matchId, CancellationToken cancellationToken)
        {
            var query = from fight in _db.Fights
                        where fight.MatchId == matchId
                           && (fight.Status == "scheduled" || fight.Status == "confirmed")
                        select fight;
            return query.AnyAsync(cancellationToken);
        }

        public void Add(Fight fight)
        {
            _db.Fights.Add(fight);
        }

        public Task SaveAsync(CancellationToken cancellationToken)
        {
            return _db.SaveChangesAsync(cancellationToken);
        }
    }
}
