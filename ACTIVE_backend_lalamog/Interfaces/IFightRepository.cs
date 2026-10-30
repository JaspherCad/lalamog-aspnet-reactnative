using Lalamog.Api.Models;

namespace Lalamog.Api.Interfaces
{
    public interface IFightRepository
    {
        Task<Fight?> GetAsync(long id, CancellationToken cancellationToken);

        Task<List<Fight>> ListAsync(Guid userId, CancellationToken cancellationToken);

        Task<bool> HasUpcomingAsync(long matchId, CancellationToken cancellationToken);

        void Add(Fight fight);

        Task SaveAsync(CancellationToken cancellationToken);
    }
}
