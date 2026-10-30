using Lalamog.Api.DTOs;

namespace Lalamog.Api.Interfaces
{
    public interface IFileStorage
    {
        Task<string> SavePhotoAsync(IFormFile file, CancellationToken cancellationToken);

        Task<StoredFile> OpenAsync(string key, CancellationToken cancellationToken);

        Task DeleteAsync(string key, CancellationToken cancellationToken);
    }
}
