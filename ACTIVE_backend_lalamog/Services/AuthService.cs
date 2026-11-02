using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Lalamog.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nickname) || request.Nickname.Trim().Length < 2)
            {
                throw new ApiException(400, "Enter a nickname of at least 2 characters.");
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = request.Email.Trim(),
                UserName = request.Email.Trim(),
                Profile = new FighterProfile
                {
                    Nickname = request.Nickname.Trim()
                }
            };

            if (request.FighterName != null)
            {
                string name = request.FighterName.Trim();
                user.Profile.NormalizedFighterName = FighterNames.Normalize(name);
                user.Profile.FighterName = name;
            }

            IdentityResult result;
            try
            {
                result = await _userManager.CreateAsync(user, request.Password);
            }
            catch (DbUpdateException exception)
            {
                if (exception.InnerException is SqlException sqlException && (sqlException.Number == 2601 || sqlException.Number == 2627))
                {
                    throw new ApiException(409, FighterNames.TakenMessage);
                }
                throw;
            }
            if (!result.Succeeded)
            {
                var errors = new List<string>();
                foreach (IdentityError error in result.Errors)
                {
                    errors.Add(error.Description);
                }
                throw new ApiException(400, string.Join(" ", errors));
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return new AuthResponse { UserId = user.Id };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            ApplicationUser? user = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (user == null)
            {
                throw new ApiException(401, "Invalid email or password, or account temporarily locked.");
            }

            SignInResult result = await _signInManager.PasswordSignInAsync(
                user, request.Password, isPersistent: false, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                throw new ApiException(401, "Invalid email or password, or account temporarily locked.");
            }

            return new AuthResponse { UserId = user.Id };
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}
