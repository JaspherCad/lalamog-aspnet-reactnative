using Microsoft.AspNetCore.Identity;
using MyApi.DTOs;
using MyApi.Models;
using MyApi.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite;
using NpgsqlTypes;
using MyApi.Interfaces;

namespace MyApi.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;
        private readonly ApplicationDbContext _Databasecontext;

        public UserService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _Databasecontext = context;
        }

        public async Task<IdentityResult> RegisterAsync(RegisterDto registerDto)
        //use native account to create user THEN create profile too./
        {
            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FullName = registerDto.FullName,
                BirthDate = registerDto.BirthDate
            };

            //user manager registers user, sign in manager of course signs in (probably handles hashing password, validation, etc.)
            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                var profile = new Profile
                {
                    UserId = user.Id,
                    Nickname = registerDto.FullName?.Split(' ').FirstOrDefault(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _Databasecontext.Profiles.Add(profile);
                await _Databasecontext.SaveChangesAsync();
            }

            return result;
        }

        public async Task<ProfileDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null) return null;

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded) return null;

            var token = _jwtService.GenerateToken(user.Id.ToString(), user.Email!, user.FullName ?? user.UserName!);

            var response = await GetProfileAsync(user.Id);
            if (response != null)
            {
                // Include the JWT token in the profile response
                response.JwtToken = token;
            }

            return response;
        }


        public async Task<ImageDto?> UploadUserProfileImageAsync(Guid userGuid, IFormFile file)
        {

            try
            {



                // Validate file size (max 5MB)
                const long maxFileSize = 5 * 1024 * 1024; // 5MB
                if (file.Length > maxFileSize)
                    throw new ArgumentException("File size cannot exceed 5MB");

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };



                // Path.GetExtension("file.jpg")       // returns ".jpg"
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                    throw new ArgumentException("Invalid file type. Only JPG, PNG, GIF, and WebP files are allowed");

                if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                    throw new ArgumentException("Invalid file content type");

                // Create upload directory if it doesn't exist
                // User-based folder structure for better organization
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profile-images", userGuid.ToString());
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                // Generate unique filename to prevent conflicts
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var uniqueFileName = $"profile_{timestamp}{fileExtension}";

                // Full path: PROJECTS\dotnet\web api fight\MyApi\wwwroot\uploads\profile-images\{userGuid}\{uniqueFileName}
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                // Get current profile to check for existing image
                var currentProfile = await GetProfileAsync(userGuid);
                string oldImagePath = null;

                // If user has an existing profile image, prepare to delete it
                if (currentProfile?.ProfilePictureUrl != null && currentProfile.ProfilePictureUrl.Contains("/uploads/profile-images/"))
                {
                    var oldFileName = Path.GetFileName(new Uri(currentProfile.ProfilePictureUrl).LocalPath);
                    // Old image should be in the same user folder
                    oldImagePath = Path.Combine(uploadsPath, oldFileName);
                }

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Generate URL for the uploaded file (relative path with user folder)
                var fileUrl = $"/uploads/profile-images/{userGuid}/{uniqueFileName}";

                Console.WriteLine(fileUrl);


                // Update user profile with new image URL ONLY
                // (to save the file URL in the database of that USER)
                var profile = await _Databasecontext.Profiles
                    .FirstOrDefaultAsync(p => p.UserId == userGuid);

                if (profile == null)
                {
                    // If profile update fails, delete the uploaded file
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);

                    return new ImageDto
                    {
                        Message = "Profile not found",
                        ImageUrl = string.Empty,
                        FileName = string.Empty,
                        FileSize = 0
                    };
                }

                // Update ONLY the profile picture URL, keeping all other data intact
                profile.ProfilePictureUrl = fileUrl;
                profile.UpdatedAt = DateTime.UtcNow;

                await _Databasecontext.SaveChangesAsync();

                // Delete old profile image if it exists and update was successful
                if (!string.IsNullOrEmpty(oldImagePath) && System.IO.File.Exists(oldImagePath))
                {
                    try
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                    catch (Exception deleteEx)
                    {
                        Console.WriteLine($"Warning: Could not delete old profile image: {deleteEx.Message}");
                        // Don't fail the request if old image deletion fails
                    }
                }


                return new ImageDto
                {
                    Message = "Profile image uploaded successfully",
                    ImageUrl = fileUrl,
                    FileName = uniqueFileName,
                    FileSize = file.Length
                };

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading profile image: {ex.Message}");
                return new ImageDto
                {
                    Message = $"Error uploading profile image: {ex.Message}",
                    ImageUrl = string.Empty,
                    FileName = string.Empty,
                    FileSize = 0
                };
            }

        }
        public async Task<UserDto?> GetUserByIdAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return null;

            return new UserDto
            {
                Id = user.Id.ToString(),
                Email = user.Email!,
                Name = user.FullName ?? user.UserName!
            };
        }

        public async Task<ProfileDto?> GetProfileAsync(Guid userId)
        {
            var profile = await _Databasecontext.Profiles
                .Include(p => p.Availability)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
                return null;

            return new ProfileDto
            {
                Id = profile.Id,
                UserId = profile.UserId,
                Nickname = profile.Nickname,
                Bio = profile.Bio,
                Location = profile.Location != null
                    ? new LocationDto { X = profile.Location.X, Y = profile.Location.Y }
                    : null,
                FightingStyle = profile.FightingStyle,
                ExperienceLevel = profile.ExperienceLevel,
                ProfilePictureUrl = profile.ProfilePictureUrl,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt,

                // Map Availability model to DTO
                Availability = profile.Availability != null
                    ? new DTOs.Availability
                    {
                        Days = profile.Availability.Days,
                        Time = profile.Availability.Time
                    }
                    : null
            };
        }

        public async Task<ProfileDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto updateDto)
        // ⚠️ WARNING: IF we are going to use prefill everything that are existing in the profile 
        {
            var profile = await _Databasecontext.Profiles
                .Include(p => p.Availability) // Include the related availability
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                // Create profile if it doesn't exist
                profile = new Profile
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _Databasecontext.Profiles.Add(profile);
            }

            // Update profile fields
            profile.Nickname = updateDto.Nickname;
            profile.Bio = updateDto.Bio;
            profile.ProfilePictureUrl = updateDto.ProfilePictureUrl;
            profile.FightingStyle = updateDto.FightingStyle;
            profile.ExperienceLevel = updateDto.ExperienceLevel;
            profile.UpdatedAt = DateTime.UtcNow;

            // Handle Availability as a separate entity
            if (updateDto.Availability != null)
            {
                if (profile.Availability == null)
                {
                    // Create new availability record
                    profile.Availability = new Models.Availability
                    {
                        UserId = userId,
                        Days = updateDto.Availability.Days,
                        Time = updateDto.Availability.Time,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    // Update existing availability
                    profile.Availability.Days = updateDto.Availability.Days;
                    profile.Availability.Time = updateDto.Availability.Time;
                    profile.Availability.UpdatedAt = DateTime.UtcNow;
                }
            }


            //#note: location point
            if (updateDto.Location != null)
            {

                // using NetTopologySuite.Geometries;
                //do the boilerplate to convert LocationDto to Point
                var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);


                //new Coordinate(longitude, latitude)
                profile.Location = geometryFactory.CreatePoint(new Coordinate(
                    updateDto.Location.X, // longitude
                    updateDto.Location.Y  // latitude
                ));
            }

            await _Databasecontext.SaveChangesAsync();

            return new ProfileDto
            {
                Id = profile.Id,
                UserId = profile.UserId,
                Nickname = profile.Nickname,
                Bio = profile.Bio,
                FightingStyle = profile.FightingStyle,
                ExperienceLevel = profile.ExperienceLevel,
                ProfilePictureUrl = profile.ProfilePictureUrl,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt,
                Location = profile.Location != null
                    ? new LocationDto { X = profile.Location.X, Y = profile.Location.Y }
                    : null,

                // Convert Model Availability to DTO Availability
                Availability = profile.Availability != null
                    ? new DTOs.Availability
                    {
                        Days = profile.Availability.Days,
                        Time = profile.Availability.Time
                    }
                    : null
            };
        }

        public async Task CreateProfileForUserAsync(Guid userId, string? displayName = null)
        {
            // Check if profile already exists
            var existingProfile = await _Databasecontext.Profiles
                .FirstOrDefaultAsync(p => p.UserId == userId);


            if (existingProfile == null)
            {
                var profile = new Profile
                {
                    UserId = userId,
                    Nickname = displayName?.Split(' ').FirstOrDefault(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _Databasecontext.Profiles.Add(profile);
                await _Databasecontext.SaveChangesAsync();

                Console.WriteLine($"Profile created for user ID: {userId}");

            }
            else
            {
                Console.WriteLine($"Profile already exists for user ID: {userId}");
            }
        }
    }
}
