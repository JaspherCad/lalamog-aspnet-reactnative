using Lalamog.Api.Contracts;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Models;
using Microsoft.AspNetCore.Identity;
namespace Lalamog.Api.Services;
public class AuthService(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nickname) || request.Nickname.Trim().Length < 2)
            throw new ApiException(400, "Enter a nickname of at least 2 characters.");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), Email = request.Email.Trim(), UserName = request.Email.Trim(),
            Profile = new FighterProfile { Nickname = request.Nickname.Trim() }
        };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new ApiException(400, string.Join(" ", result.Errors.Select(x => x.Description)));
        await signIn.SignInAsync(user, isPersistent: false);
        return new(user.Id);
    }
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null) throw new ApiException(401, "Invalid email or password, or account temporarily locked.");
        var result = await signIn.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: true);
        if (!result.Succeeded) throw new ApiException(401, "Invalid email or password, or account temporarily locked.");
        return new(user.Id);
    }
    public Task LogoutAsync() => signIn.SignOutAsync();
}