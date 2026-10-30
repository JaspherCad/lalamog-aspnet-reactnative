using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyApi.DTOs;
using MyApi.Services;
using MyApi.Models;
using MyApi.Interfaces;
using System.Security.Claims;




// THIS AUTH IS PURELY FOR OAUTH..
// USERCONTROLLER.cs handles the native user registration and login. also we just call those functions here for google users to create a profile and get a JWT token.






namespace MyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtService _jwtService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserService _userService;

        public AuthController(IJwtService jwtService, UserManager<ApplicationUser> userManager, IUserService userService)
        {
            _jwtService = jwtService;
            _userManager = userManager;
            _userService = userService;
        }

        [HttpGet("google")] //open the google login page (di ko gawa)
        public IActionResult GoogleLogin()
        {
            var authProperties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleCallback", "Auth")
            };

            return Challenge(authProperties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google/callback")]
        //**IMPORTANT** 
        // (WE DONT RETURN JWT of "existing user on our NATIVE APP")
        // instead we get the google user info and create a profile for them in our native app.


        // end goal of this is simply to generate JWT using google info IF successful. then redirect the jwt in URL.
        public async Task<IActionResult> GoogleCallback()
        {
            //return jwt claims: to get email name googleId FROM GOOGLE ACCOUNT (not our native app)
            //use those values to CREATE NATIVE USER ACCOUNT

            // BASE CASE: if existingUser on our NATIVE APP is not found then create _userManager.CreateAsync(newUser);  

            // hit this: _userService.CreateProfileForUserAsync(existingUser.Id, name); --this creates PROFILE for the user in our native app.. if already true do nothing

            // if profile and existing user is ok, return JWT
                //_jwtService.GenerateToken





            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            Console.WriteLine("result : ", result);

            if (!result.Succeeded)
            {
                return Redirect("/?error=authentication_failed");
            }

            var claims = result.Principal?.Claims.ToList();
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var googleId = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(googleId))
            {
                return Redirect("/?error=missing_user_data");
            }

            // Check if user already exists in our database
            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser == null)
            {
                // Create new user in our database for Google login
                var newUser = new ApplicationUser
                {   //values from google CLAIMS
                    UserName = email,
                    Email = email,
                    FullName = name,
                    EmailConfirmed = true // Google emails are already confirmed
                };

                var createResult = await _userManager.CreateAsync(newUser);

                if (!createResult.Succeeded)
                {
                    return Redirect("/?error=user_creation_failed");
                }


                existingUser = newUser;
            }

            // Create profile for the new Google user
            await _userService.CreateProfileForUserAsync(existingUser.Id, name);

            // Generate JWT token using our native user
            var token = _jwtService.GenerateToken(existingUser.Id.ToString(), existingUser.Email!, existingUser.FullName ?? existingUser.UserName!);

            // Redirect to frontend with token and user info
            var redirectUrl = $"/?token={Uri.EscapeDataString(token)}&id={Uri.EscapeDataString(existingUser.Id.ToString())}&email={Uri.EscapeDataString(existingUser.Email!)}&name={Uri.EscapeDataString(existingUser.FullName ?? existingUser.UserName!)}";
            return Redirect(redirectUrl);
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
    }
}
