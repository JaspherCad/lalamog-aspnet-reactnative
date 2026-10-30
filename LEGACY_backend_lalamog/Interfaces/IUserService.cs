using Microsoft.AspNetCore.Identity;
using MyApi.DTOs;

namespace MyApi.Interfaces
{
    public interface IUserService
    {
        Task<IdentityResult> RegisterAsync(RegisterDto registerDto);
        Task<ProfileDto?> LoginAsync(LoginDto loginDto);
        Task<UserDto?> GetUserByIdAsync(Guid userId);
        Task<ProfileDto?> GetProfileAsync(Guid userId);
        Task<ProfileDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto updateDto);

        Task<ImageDto?> UploadUserProfileImageAsync(Guid userId, IFormFile file);
        Task CreateProfileForUserAsync(Guid userId, string? displayName = null);
    }
}
