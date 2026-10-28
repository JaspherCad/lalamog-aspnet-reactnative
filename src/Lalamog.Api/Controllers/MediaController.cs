using Lalamog.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
namespace Lalamog.Api.Controllers;
[ApiController, Route("api/media")]
public class MediaController(IFileStorage files) : ControllerBase
{
    [HttpGet("{**key}")]
    public async Task<IActionResult> Get(string key, CancellationToken ct)
    {
        var file = await files.OpenAsync(key, ct);
        Response.Headers.CacheControl = "public,max-age=86400,immutable";
        return File(file.Content, file.ContentType);
    }
}