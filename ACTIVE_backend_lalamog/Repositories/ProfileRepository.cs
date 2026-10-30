using Lalamog.Api.Data;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lalamog.Api.Repositories
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly AppDbContext _db;

        public ProfileRepository(AppDbContext db)
        {
            _db = db;
        }

        public Task<FighterProfile?> GetAsync(Guid userId, CancellationToken cancellationToken)
        {
            var query = from profile in _db.Profiles
                        where profile.UserId == userId
                        select profile;
            return query.SingleOrDefaultAsync(cancellationToken);
        }

        public Task<List<FighterProfile>> DiscoverAsync(Guid userId, CancellationToken cancellationToken)
        {
            var query = from profile in _db.Profiles.AsNoTracking()
                        where profile.UserId != userId
                        where !(from swipe in _db.Swipes
                                where swipe.SwiperId == userId && swipe.TargetId == profile.UserId
                                select swipe).Any()
                        where !(from match in _db.Matches
                                where (match.User1Id == userId && match.User2Id == profile.UserId)
                                   || (match.User2Id == userId && match.User1Id == profile.UserId)
                                select match).Any()
                        orderby profile.Nickname, profile.UserId
                        select profile;
            return query.Take(50).ToListAsync(cancellationToken);
        }

        public Task SaveAsync(CancellationToken cancellationToken)
        {
            return _db.SaveChangesAsync(cancellationToken);
        }
    }
}
