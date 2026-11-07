using System.Security.Claims;
using Lalamog.Api.DTOs;
using Lalamog.Api.Authorization;
using Lalamog.Api.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lalamog.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IAccessService _access;
        private readonly IAntiforgery _antiforgery;

        public AuthController(IAuthService authService, IAntiforgery antiforgery, IAccessService access)
        {
            _authService = authService;
            _access = access;
            _antiforgery = antiforgery;
        }

        [HttpGet("csrf")]
        public ActionResult<CsrfResponse> Csrf()
        {
            Response.Headers.CacheControl = "no-store";
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            var response = new CsrfResponse { Token = tokens.RequestToken };
            return Ok(response);
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            AuthResponse response = await _authService.RegisterAsync(request);
            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            AuthResponse response = await _authService.LoginAsync(request);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<AuthResponse>> Me(CancellationToken cancellationToken)
        {
            return Ok(await _access.SessionAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), cancellationToken));
        }

        [Authorize(Policy = ApplicationPermissions.Owner_EnableFighter)]
        [HttpPost("enable-fighter")]
        public async Task<ActionResult<AuthResponse>> EnableFighter(CancellationToken cancellationToken)
        {
            Guid actor = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _access.EnableFighterAsync(actor, cancellationToken);
            return Ok(await _access.SessionAsync(actor, cancellationToken));
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return NoContent();
        }
    }
}
