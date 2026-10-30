using Lalamog.Api.DTOs;

namespace Lalamog.Api.Interfaces
{
    public interface IFightService
    {
        Task<List<FightResponse>> ListAsync(Guid actor, CancellationToken cancellationToken);

        Task<FightResponse> GetAsync(Guid actor, long id, CancellationToken cancellationToken);

        Task<FightResponse> ScheduleAsync(Guid actor, ScheduleRequest request, CancellationToken cancellationToken);

        Task<FightResponse> ConfirmAsync(Guid actor, long id, ConfirmRequest request, CancellationToken cancellationToken);

        Task<FightResponse> CancelAsync(Guid actor, long id, CancelRequest request, CancellationToken cancellationToken);

        Task<FightResponse> CompleteAsync(Guid actor, long id, CancellationToken cancellationToken);
    }
}
