using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.DTOs;
using MyApi.Services;
using MyApi.Interfaces;
using System.Security.Claims;

namespace MyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SwipeController : ControllerBase
    {
        private readonly ISwipeService _swipeService;
        private readonly IUserService _userService;

        public SwipeController(ISwipeService swipeService, IUserService userService)
        {
            _swipeService = swipeService;
            _userService = userService;
        }

        [HttpGet("available-profiles")]
        [Authorize]
        public async Task<IActionResult> GetAvailableProfiles()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            if (!Guid.TryParse(userId, out Guid parsedUserId))
            {
                return BadRequest(new { message = "Invalid user ID format" });
            }

            var profiles = await _swipeService.FetchAllAvailableProfilesAsync(parsedUserId);
            return Ok(profiles);
        }


        //  [HttpGet("all-profiles")]

        //   "matchedProfiles": [
        //     {
        //       "id": "...",
        //       "userId": "...",
        //       "nickname": "...",
        //       "bio": "...",
        //     }
        //   ],
        //   "availableProfiles": [
        //     {
        //       "id": "...",
        //       "userId": "...",
        //       "nickname": "...",
        //       "bio": "...",
        //     }
        //   ]
        // }    [HttpGet("all-profiles")]

        [HttpGet("all-profiles")]
        [Authorize]
        public async Task<IActionResult> GetAllProfiles()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            if (!Guid.TryParse(userId, out Guid parsedUserId))
            {
                return BadRequest(new { message = "Invalid user ID format" });
            }

            var profiles = await _swipeService.FetchAllProfilesAsync(parsedUserId);
            // returns:
            //  public List<ProfileDto> MatchedProfiles { get; set; } = new List<ProfileDto>();
            //  public List<ProfileDto> AvailableProfiles { get; set; } = new List<ProfileDto>();
            return Ok(profiles);
        }



        // GetAllMatchesForUser
        [HttpGet("all-matches-data")]
        [Authorize]
        public async Task<IActionResult> GetAllMatchesForUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            if (!Guid.TryParse(userId, out Guid parsedUserId))
            {
                return BadRequest(new { message = "Invalid user ID format" });
            }

            var matches = await _swipeService.GetAllMatchesForUser(parsedUserId);
            return Ok(matches);
        }

        [HttpGet("all-matched-profiles")]
        [Authorize]
        public async Task<IActionResult> GetAllMatchedProfiles()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            if (!Guid.TryParse(userId, out Guid parsedUserId))
            {
                return BadRequest(new { message = "Invalid user ID format" });
            }

            var profiles = await _swipeService.GetMatchedUserOfUserWithID(parsedUserId);
            // returns:
            //  public List<ProfileDto> MatchedProfiles { get; set; } = new List<ProfileDto>();
            
            return Ok(profiles);
        }

        [HttpPost("swipes")]
        [Authorize]
        // public Guid SwipeeId { get; set; }

        // [Required]
        // [RegularExpression("right|left", ErrorMessage = "Direction must be 'right' or 'left'")]
        // public string Direction { get; set; } = "right";
        public async Task<IActionResult> Swipe([FromBody] SwipeDto swipeDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            if (!Guid.TryParse(userId, out Guid swiperId))
            {
                return BadRequest(new { message = "Invalid user ID format" });
            }

            try
            {
                var result = await _swipeService.ProcessSwipeAsync(swiperId, swipeDto.SwipeeId, swipeDto.Direction);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return BadRequest(new { message = "Failed to record swipe" });
            }
        }
    }
}