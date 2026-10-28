using Lalamog.Api.Contracts;
using Lalamog.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace Lalamog.Api.Controllers;
[Route("api/profiles")]
public class ProfilesController(ProfileService profiles) : ApiController
{
    [HttpGet("me")]
    public Task<ProfileResponse> Me(CancellationToken ct) => profiles.GetAsync(ActorId, ct);
    [HttpPut("me")]
    public Task<ProfileResponse> Update(ProfileRequest request, CancellationToken ct) => profiles.UpdateAsync(ActorId, request, ct);
    [HttpGet("discover")]
    public Task<List<ProfileResponse>> Discover(CancellationToken ct) => profiles.DiscoverAsync(ActorId, ct);
    [HttpPost("me/avatar"), RequestSizeLimit(6 * 1024 * 1024)]
    public Task<ProfileResponse> Avatar(IFormFile file, CancellationToken ct) => profiles.AvatarAsync(ActorId, file, ct);
}