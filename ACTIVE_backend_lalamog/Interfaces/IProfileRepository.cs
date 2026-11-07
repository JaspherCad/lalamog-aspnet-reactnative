using Lalamog.Api.Models;
using Lalamog.Api.DTOs;

namespace Lalamog.Api.Interfaces
{
    public interface IProfileRepository
    {
        Task<FighterProfile?> GetAsync(Guid userId, CancellationToken cancellationToken);

        Task<List<FighterProfile>> DiscoverAsync(Guid userId, CancellationToken cancellationToken);

        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<LegacyAvatar>> GetLegacyAvatarsAsync(CancellationToken cancellationToken);

        Task ClearLegacyAvatarAsync(LegacyAvatar avatar, CancellationToken cancellationToken);
    }
}
