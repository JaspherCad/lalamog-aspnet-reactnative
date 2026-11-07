using Lalamog.Api.Data;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Lalamog.Api.DTOs;
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
            var query = from profile in _db.Profiles.Include(nameof(FighterProfile.User))
                        where profile.UserId == userId
                        select profile;
            return query.SingleOrDefaultAsync(cancellationToken);
        }

        public Task<List<FighterProfile>> DiscoverAsync(Guid userId, CancellationToken cancellationToken)
        {
            var query = from profile in _db.Profiles.AsNoTracking()
                        where (from link in _db.UserRoles
                               join grant in _db.RoleSystemFeatures on link.RoleId equals grant.RoleId
                               where link.UserId == profile.UserId && grant.SystemFeature.Code == Lalamog.Api.Authorization.ApplicationPermissions.User_Match
                               select link).Any()
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

        public Task<List<LegacyAvatar>> GetLegacyAvatarsAsync(CancellationToken cancellationToken)
        {
            // Dynamic SQL avoids referencing a column that may already have been removed.
            return _db.Database.SqlQueryRaw<LegacyAvatar>("""
                IF COL_LENGTH('Profiles', 'AvatarBlobName') IS NOT NULL
                    EXEC('SELECT UserId, AvatarBlobName AS BlobName FROM Profiles WHERE AvatarBlobName IS NOT NULL');
                ELSE IF COL_LENGTH('Profiles', 'AvatarKey') IS NOT NULL
                    EXEC('SELECT UserId, AvatarKey AS BlobName FROM Profiles WHERE AvatarKey IS NOT NULL');
                ELSE
                    SELECT CAST(NULL AS uniqueidentifier) AS UserId, CAST(NULL AS nvarchar(100)) AS BlobName WHERE 1 = 0;
                """).ToListAsync(cancellationToken);
        }

        public Task ClearLegacyAvatarAsync(LegacyAvatar avatar, CancellationToken cancellationToken)
        {
            // Clear only the exact reference that has just been safely copied to storage.
            return _db.Database.ExecuteSqlInterpolatedAsync($"""
                IF COL_LENGTH('Profiles', 'AvatarBlobName') IS NOT NULL
                    EXEC sp_executesql N'UPDATE Profiles SET AvatarBlobName = NULL WHERE UserId = @id AND AvatarBlobName = @name',
                        N'@id uniqueidentifier, @name nvarchar(100)', @id = {avatar.UserId}, @name = {avatar.BlobName};
                ELSE IF COL_LENGTH('Profiles', 'AvatarKey') IS NOT NULL
                    EXEC sp_executesql N'UPDATE Profiles SET AvatarKey = NULL WHERE UserId = @id AND AvatarKey = @name',
                        N'@id uniqueidentifier, @name nvarchar(100)', @id = {avatar.UserId}, @name = {avatar.BlobName};
                """, cancellationToken);
        }
    }
}
