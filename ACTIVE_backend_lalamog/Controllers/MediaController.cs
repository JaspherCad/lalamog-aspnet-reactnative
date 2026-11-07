using Lalamog.Api.DTOs;
using Lalamog.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lalamog.Api.Controllers
{
    [ApiController]
    [Route("api/media")]
    public class MediaController : ControllerBase
    {
        private readonly IFileStorage _fileStorage;
        private readonly IProfileService _profiles;

        public MediaController(IFileStorage fileStorage, IProfileService profiles)
        {
            _fileStorage = fileStorage;
            _profiles = profiles;
        }

        [HttpGet("avatars/{userId:guid}")]
        public async Task<IActionResult> Avatar(Guid userId, CancellationToken cancellationToken)
        {
            StoredFile file = await _profiles.OpenAvatarAsync(userId, cancellationToken);
            Response.Headers.CacheControl = "no-store";
            Response.Headers.XContentTypeOptions = "nosniff";
            return File(file.Content, file.ContentType);
        }

        [HttpGet("{**blobName}")]
        public async Task<IActionResult> Get(string blobName, CancellationToken cancellationToken)
        {
            StoredFile file = await _fileStorage.OpenAsync(blobName, cancellationToken);
            Response.Headers.CacheControl = blobName.StartsWith("avatars/", StringComparison.Ordinal)
                ? "no-store" : "public,max-age=86400,immutable";
            Response.Headers.XContentTypeOptions = "nosniff";
            return File(file.Content, file.ContentType);
        }
    }
}
