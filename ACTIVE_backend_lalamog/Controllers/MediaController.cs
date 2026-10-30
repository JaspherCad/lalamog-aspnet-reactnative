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

        public MediaController(IFileStorage fileStorage)
        {
            _fileStorage = fileStorage;
        }

        [HttpGet("{**key}")]
        public async Task<IActionResult> Get(string key, CancellationToken cancellationToken)
        {
            StoredFile file = await _fileStorage.OpenAsync(key, cancellationToken);
            Response.Headers.CacheControl = "public,max-age=86400,immutable";
            return File(file.Content, file.ContentType);
        }
    }
}
