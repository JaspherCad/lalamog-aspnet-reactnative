using System.Security.Claims;
using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Lalamog.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TimeProvider _clock;

    public AuthService(IUserRepository users, IHttpContextAccessor httpContextAccessor, TimeProvider clock)
    {
        _users = users;
        _httpContextAccessor = httpContextAccessor;
        _clock = clock;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nickname) || request.Nickname.Trim().Length < 2)
        {
            throw new ApiException(400, "Enter a nickname of at least 2 characters.");
        }
        ValidatePassword(request.Password);
        string email = request.Email.Trim();
        string normalizedEmail = email.ToUpperInvariant();
        if (await _users.EmailExistsAsync(normalizedEmail))
        {
            throw new ApiException(400, "An account with this email already exists.");
        }

        var user = new AppUser
        {
            Email = email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = PasswordHashing.Hash(request.Password),
            Profile = new FighterProfile { Nickname = request.Nickname.Trim() }
        };
        if (request.FighterName != null)
        {
            string name = request.FighterName.Trim();
            user.Profile.NormalizedFighterName = FighterNames.Normalize(name);
            user.Profile.FighterName = name;
        }
        try
        {
            await _users.AddAsync(user);
        }
        catch (DbUpdateException exception)
        {
            if (exception.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
            {
                if (await _users.EmailExistsAsync(normalizedEmail))
                {
                    throw new ApiException(400, "An account with this email already exists.");
                }
                throw new ApiException(409, FighterNames.TakenMessage);
            }
            throw;
        }

        await SignInAsync(user);
        return new AuthResponse { UserId = user.Id };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        string normalizedEmail = request.Email.Trim().ToUpperInvariant();
        for (int attempt = 0; attempt < 3; attempt++)
        {
            AppUser? user = await _users.FindByEmailAsync(normalizedEmail);
            DateTimeOffset now = _clock.GetUtcNow();
            if (user == null || user.LockoutEnd > now)
            {
                throw InvalidLogin();
            }

            bool valid = PasswordHashing.Verify(user.PasswordHash, request.Password);
            if (user.LockoutEnd.HasValue)
            {
                user.AccessFailedCount = 0;
                user.LockoutEnd = null;
            }
            if (valid)
            {
                user.AccessFailedCount = 0;
            }
            else
            {
                user.AccessFailedCount++;
                if (user.AccessFailedCount >= 5)
                {
                    user.LockoutEnd = now.AddMinutes(5);
                }
            }
            try
            {
                await _users.UpdateAsync(user);
            }
            catch (DbUpdateConcurrencyException)
            {
                continue;
            }
            if (!valid)
            {
                throw InvalidLogin();
            }
            await SignInAsync(user);
            return new AuthResponse { UserId = user.Id };
        }
        throw InvalidLogin();
    }

    public Task LogoutAsync()
    {
        return GetHttpContext().SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private Task SignInAsync(AppUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email ?? ""),
            new Claim(ClaimTypes.Email, user.Email ?? "")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        return GetHttpContext().SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            principal, new AuthenticationProperties { IsPersistent = false });
    }

    private HttpContext GetHttpContext()
    {
        return _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Authentication requires an HTTP request.");
    }

    private static ApiException InvalidLogin()
    {
        return new ApiException(401, "Invalid email or password, or account temporarily locked.");
    }

    private static void ValidatePassword(string password)
    {
        bool lower = false;
        bool upper = false;
        bool digit = false;
        bool symbol = false;
        foreach (char character in password)
        {
            lower |= char.IsLower(character);
            upper |= char.IsUpper(character);
            digit |= char.IsDigit(character);
            symbol |= !char.IsLetterOrDigit(character);
        }
        if (password.Length < 10 || password.Length > 128 || !lower || !upper || !digit || !symbol)
        {
            throw new ApiException(400, "Use 10 to 128 characters with uppercase, lowercase, a number and a symbol.");
        }
    }
}
