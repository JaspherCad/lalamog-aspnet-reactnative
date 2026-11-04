using Lalamog.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Lalamog.Api.DTOs;
using Lalamog.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lalamog.Api.Controllers
{
    [Authorize(Policy = ApplicationPermissions.User_View)]
    [Route("api/profiles")]
    public class ProfilesController : ApiController
    {
        private readonly IProfileService _profileService;

        public ProfilesController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet("me")]
        public async Task<ActionResult<ProfileResponse>> Me(CancellationToken cancellationToken)
        {
            ProfileResponse response = await _profileService.GetAsync(ActorId, cancellationToken);
            return Ok(response);
        }

        [HttpPut("me")]
        [Authorize(Policy = ApplicationPermissions.User_EditProfile)]
        public async Task<ActionResult<ProfileResponse>> Update(ProfileRequest request, CancellationToken cancellationToken)
        {
            ProfileResponse response = await _profileService.UpdateAsync(ActorId, request, cancellationToken);
            return Ok(response);
        }

        [HttpGet("discover")]
        public async Task<ActionResult<List<ProfileResponse>>> Discover(CancellationToken cancellationToken)
        {
            List<ProfileResponse> response = await _profileService.DiscoverAsync(ActorId, cancellationToken);
            return Ok(response);
        }

        [HttpPost("me/avatar")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        [Authorize(Policy = ApplicationPermissions.User_Upload)]
        public async Task<ActionResult<ProfileResponse>> Avatar(IFormFile file, CancellationToken cancellationToken)
        {
            ProfileResponse response = await _profileService.AvatarAsync(ActorId, file, cancellationToken);
            return Ok(response);
        }
    }
}
