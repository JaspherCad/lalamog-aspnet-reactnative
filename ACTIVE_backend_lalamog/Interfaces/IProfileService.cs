using Lalamog.Api.DTOs;

namespace Lalamog.Api.Interfaces
{
    public interface IProfileService
    {
        Task<ProfileResponse> GetAsync(Guid userId, CancellationToken cancellationToken);

        Task<List<ProfileResponse>> DiscoverAsync(Guid userId, CancellationToken cancellationToken);

        Task<ProfileResponse> UpdateAsync(Guid userId, ProfileRequest request, CancellationToken cancellationToken);

        Task<ProfileResponse> AvatarAsync(Guid userId, IFormFile file, CancellationToken cancellationToken);

        Task<StoredFile> OpenAvatarAsync(Guid userId, CancellationToken cancellationToken);

        Task MigrateLegacyAvatarsAsync(CancellationToken cancellationToken);
    }
}
