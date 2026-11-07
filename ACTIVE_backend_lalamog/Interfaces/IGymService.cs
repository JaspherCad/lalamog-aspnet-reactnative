using Lalamog.Api.DTOs;
namespace Lalamog.Api.Interfaces;
public interface IGymService
{
    Task<List<GymResponse>> ListAsync(CancellationToken cancellationToken);
    Task<GymResponse> GetAsync(long id, Guid? actor, CancellationToken cancellationToken);
    Task<List<GymResponse>> OwnedAsync(Guid actor, CancellationToken cancellationToken);
    Task<GymResponse> CreateAsync(Guid actor, GymRequest request, CancellationToken cancellationToken);
    Task<GymResponse> UpdateAsync(Guid actor, long id, GymRequest request, CancellationToken cancellationToken);
    Task<GymResponse> PublishAsync(Guid actor, long id, bool publish, CancellationToken cancellationToken);
    Task<List<GymBookingResponse>> BookingsAsync(Guid actor, CancellationToken cancellationToken);
    Task<GymBookingResponse> DecideAsync(Guid actor, long fightId, string action, string? reason, CancellationToken cancellationToken);
    Task UploadCoverAsync(Guid actor, long id, IFormFile file, CancellationToken cancellationToken);
    Task<StoredFile> CoverAsync(long id, Guid? actor, CancellationToken cancellationToken);
    Task<List<AdminGymResponse>> AdminListAsync(CancellationToken cancellationToken);
    Task AdminUnpublishAsync(long id, CancellationToken cancellationToken);
    Task AssignOwnerAsync(long id, Guid ownerId, CancellationToken cancellationToken);
}
