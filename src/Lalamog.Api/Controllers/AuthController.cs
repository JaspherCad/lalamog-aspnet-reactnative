using System.Security.Claims;
using Lalamog.Api.Contracts;
using Lalamog.Api.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace Lalamog.Api.Controllers;
[ApiController, Route("api/auth"), EnableRateLimiting("auth")]
public class AuthController(AuthService auth, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("csrf")]
    public object Csrf()
    {
        Response.Headers.CacheControl = "no-store";
        return new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken };
    }
    [HttpPost("register")]
    public Task<AuthResponse> Register(RegisterRequest request) => auth.RegisterAsync(request);
    [HttpPost("login")]
    public Task<AuthResponse> Login(LoginRequest request) => auth.LoginAsync(request);
    [Authorize, HttpGet("me")]
    public AuthResponse Me() => new(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout() { await auth.LogoutAsync(); return NoContent(); }
}