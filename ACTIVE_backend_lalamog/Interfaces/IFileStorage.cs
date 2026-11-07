using Lalamog.Api.DTOs;

namespace Lalamog.Api.Interfaces
{
    public interface IFileStorage
    {
        Task<string> SavePhotoAsync(IFormFile file, CancellationToken cancellationToken);

        Task<StoredFile> OpenAsync(string blobName, CancellationToken cancellationToken);

        Task SetGymCoverAsync(long gymId, StoredFile photo, CancellationToken cancellationToken);

        Task SetAvatarAsync(Guid userId, StoredFile photo, bool onlyIfMissing, CancellationToken cancellationToken);

        Task DeleteAsync(string blobName, CancellationToken cancellationToken);
    }
}
