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
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }



        [HttpGet("test-connection")]
        [AllowAnonymous]
        public IActionResult TestConnection()
        {
            return Ok(new { message = "Connection to ASP.NET Web API successful!" });
        }



        [HttpPost("register")]
        //  { ---------------
        //   "email": "AlterEgo2@gmail.com",
        //   "password": "Password123!",
        //   "confirmPassword": "Password123!",
        //   "fullName": "Lance Gonzales",
        //   "birthDate": "1990-05-14T00:00:00Z"
        // }
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(new { message = "Validation failed", errors });
                }

                var result = await _userService.RegisterAsync(registerDto);

                if (result.Succeeded)
                {
                    return Ok(new { message = "User registered successfully" });
                }

                var identityErrors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new { message = "Registration failed", errors = identityErrors });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Register endpoint: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred during registration", error = ex.Message });
            }
        }



        [HttpPost("login")]
        // {
        //   "email": "user@example.com",
        //   "password": "password123"
        // }

        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(new { message = "Validation failed", errors });
                }

                var profileWithToken = await _userService.LoginAsync(loginDto);

                if (profileWithToken == null)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                return Ok(profileWithToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Login endpoint: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred during login", error = ex.Message });
            }
        }

        //postgresql command: 
        //           SELECT 

        //     ST_X("Location"::geometry) AS longitude,
        //     ST_Y("Location"::geometry) AS latitude
        //   FROM "Profiles";

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized();

            var profile = await _userService.GetProfileAsync(userGuid);
            if (profile == null)
                return NotFound(new { message = "Profile not found" });

            return Ok(profile);
        }


        [HttpGet("profile/{id}")]
        public async Task<IActionResult> GetProfileById([FromRoute] Guid id)
        {
            var profile = await _userService.GetProfileAsync(id);

            if (profile == null)
            {
                return NotFound(new { message = "Profile not found" });
            }

            return Ok(profile);
        }




        //persistent validation of token
        [HttpGet("validate")]
        [Authorize]
        public async Task<IActionResult> ValidateSession()
        {
            try
            {
                // Check if user is authenticated
                if (!User.Identity?.IsAuthenticated == true)
                {
                    return Unauthorized();
                }

                // Get user ID from JWT claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub") ?? User.FindFirst("userId");
                Console.WriteLine($"User claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}:{c.Value}"))}");

                if (userIdClaim == null)
                {
                    return Unauthorized();
                }

                if (!Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized();
                }

                // Get fresh profile data using the existing UserService
                var profile = await _userService.GetProfileAsync(userId);
                if (profile == null)
                {
                    return NotFound("Profile not found");
                }

                // add the jwt token to the response
                profile.JwtToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return Problem($"Error validating session: {ex.Message}");
            }
        }

















        // SELECT json_build_object(
        //     'ProfileId', p."Id",
        //     'UserId', p."UserId",
        //     'Nickname', p."Nickname",
        //     'Bio', p."Bio",
        //     'Location', json_build_object(
        //         'x', ST_X(p."Location"::geometry),
        //         'y', ST_Y(p."Location"::geometry)
        //     ),
        //     'FightingStyle', p."FightingStyle",
        //     'ExperienceLevel', p."ExperienceLevel",
        //     'ProfilePictureUrl', p."ProfilePictureUrl",
        //     'CreatedAt', p."CreatedAt",
        //     'UpdatedAt', p."UpdatedAt",
        //     'Availability', json_build_object(
        //         'days', a."Days",   
        //         'time', a."Time"
        //     ),
        //     'UserName', u."UserName",
        //     'Email', u."Email"
        // )
        // FROM "Profiles" p
        // JOIN "AspNetUsers" u
        //     ON p."UserId" = u."Id"
        // LEFT JOIN "Availabilities" a
        //     ON a."UserId" = p."UserId";
        [HttpPut("profile")]

        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized();

            var profile = await _userService.UpdateProfileAsync(userGuid, updateDto);
            if (profile == null)
                return BadRequest(new { message = "Failed to update profile" });

            return Ok(profile);
        }




        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(id))
            {
                return Unauthorized();
            }

            return Ok(new UserDto
            {
                Id = id,
                Email = email,
                Name = name
            });
        }


        [HttpPost("uploadprofileimage")]
        [Authorize]
        public async Task<IActionResult> UploadUserProfileImage(IFormFile file)
        {
            try
            {
                // Get user ID from JWT claims
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                    return Unauthorized();



                var uploadResult = await _userService.UploadUserProfileImageAsync(userGuid, file);

                if (uploadResult == null)
                {
                    return BadRequest(new { message = "Failed to upload profile image" });
                }

                return Ok(uploadResult); // ImageDto with upload result


                // Example response: To use this in frontend, lets do env.apiBaseUrl + ImageDTO.ImageUrl

                //Example response: {
                //     "message": "Profile image uploaded successfully",
                //     "imageUrl": "/uploads/profile-images/f2603802-deb3-4377-8651-1134db7837a7/profile_1754026747.png",
                //     "fileName": "profile_1754026747.png",
                //     "fileSize": 37197
                // }
            }
            catch (Exception ex)
            {
                return Problem($"Error uploading profile image: {ex.Message}");
            }
        }
    }
}
