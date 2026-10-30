using Lalamog.Api.Models;

namespace Lalamog.Api.Interfaces
{
    public interface IProfileRepository
    {
        Task<FighterProfile?> GetAsync(Guid userId, CancellationToken cancellationToken);

        Task<List<FighterProfile>> DiscoverAsync(Guid userId, CancellationToken cancellationToken);

        Task SaveAsync(CancellationToken cancellationToken);
    }
}
