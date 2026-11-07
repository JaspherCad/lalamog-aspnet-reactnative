using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Lalamog.Api.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IProfileRepository _profileRepository;
        private readonly IFileStorage _fileStorage;
        private readonly IAreaService _areas;

        public ProfileService(IProfileRepository profileRepository, IFileStorage fileStorage, IAreaService areas)
        {
            _profileRepository = profileRepository;
            _fileStorage = fileStorage;
            _areas = areas;
        }

        public async Task<ProfileResponse> GetAsync(Guid userId, CancellationToken cancellationToken)
        {
            FighterProfile profile = await RequireProfileAsync(userId, cancellationToken);
            return DtoMapper.ToProfileResponse(profile, true);
        }

        public async Task<List<ProfileResponse>> DiscoverAsync(Guid userId, CancellationToken cancellationToken)
        {
            List<FighterProfile> profiles = await _profileRepository.DiscoverAsync(userId, cancellationToken);
            var responses = new List<ProfileResponse>();
            foreach (FighterProfile profile in profiles)
            {
                responses.Add(DtoMapper.ToProfileResponse(profile));
            }
            return responses;
        }

        public async Task<ProfileResponse> UpdateAsync(Guid userId, ProfileRequest request, CancellationToken cancellationToken)
        {
            if (request.Nickname.Trim().Length < 2 || string.IsNullOrWhiteSpace(request.FightingStyle))
            {
                throw new ApiException(400, "Enter a nickname and fighting style.");
            }

            FighterProfile profile = await RequireProfileAsync(userId, cancellationToken);
            if (request.FighterName != null)
            {
                string fighterName = request.FighterName.Trim();
                string normalizedName = FighterNames.Normalize(fighterName);
                profile.FighterName = fighterName;
                profile.NormalizedFighterName = normalizedName;
            }
            profile.Nickname = request.Nickname.Trim();
            profile.Bio = (request.Bio ?? string.Empty).Trim();
            if (request.FightingStyle.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length > 3)
            {
                throw new ApiException(400, "Choose up to three fighting styles.");
            }
            profile.FightingStyle = request.FightingStyle.Trim();
            profile.ExperienceLevel = request.ExperienceLevel;
            profile.Location = (request.Location ?? string.Empty).Trim();
            profile.Availability = (request.Availability ?? string.Empty).Trim();
            if (request.MapLocation != null)
            {
                _areas.Validate(request.MapLocation);
                profile.RegionCode = request.MapLocation.RegionCode;
                profile.ProvinceCode = request.MapLocation.ProvinceCode;
                profile.CityCode = request.MapLocation.CityCode;
                profile.BarangayCode = request.MapLocation.BarangayCode;
                profile.Latitude = request.MapLocation.Latitude;
                profile.Longitude = request.MapLocation.Longitude;
                profile.PsgcVersion = profile.RegionCode == null ? null : _areas.Version;
                if (profile.CityCode != null)
                {
                    profile.Location = _areas.CityName(profile.CityCode);
                }
            }


            try
            {
                await _profileRepository.SaveAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                if (exception.InnerException is SqlException sqlException && (sqlException.Number == 2601 || sqlException.Number == 2627))
                {
                    throw new ApiException(409, FighterNames.TakenMessage);
                }
                throw;
            }
            return DtoMapper.ToProfileResponse(profile, true);
        }

        public async Task<ProfileResponse> AvatarAsync(Guid userId, IFormFile file, CancellationToken cancellationToken)
        {
            FighterProfile profile = await RequireProfileAsync(userId, cancellationToken);
            string blobName = await _fileStorage.SavePhotoAsync(file, cancellationToken);

            try
            {
                StoredFile photo = await _fileStorage.OpenAsync(blobName, cancellationToken);
                await using (photo.Content)
                {
                    await _fileStorage.SetAvatarAsync(userId, photo, false, cancellationToken);
                }
            }
            finally
            {
                await _fileStorage.DeleteAsync(blobName, CancellationToken.None);
            }

            var response = DtoMapper.ToProfileResponse(profile, true);
            // Refresh an already-mounted browser image after replacing its stable owner path.
            response.AvatarUrl += $"?v={Guid.NewGuid():N}";
            return response;
        }

        public async Task<StoredFile> OpenAvatarAsync(Guid userId, CancellationToken cancellationToken)
        {
            FighterProfile profile = await RequireProfileAsync(userId, cancellationToken);
            try
            {
                return await _fileStorage.OpenAsync($"avatars/{userId:N}/current", cancellationToken);
            }
            catch (ApiException exception) when (exception.StatusCode == 404)
            {
                // Only fictional seed accounts use bundled defaults; SQL stores no avatar metadata.
                string email = profile.User.Email ?? "";
                if (!email.EndsWith(".seed@example.test", StringComparison.OrdinalIgnoreCase))
                {
                    throw;
                }
                string name = email[..email.IndexOf('.')].ToLowerInvariant();
                return await _fileStorage.OpenAsync($"seed-{name}.webp", cancellationToken);
            }
        }

        public async Task MigrateLegacyAvatarsAsync(CancellationToken cancellationToken)
        {
            foreach (LegacyAvatar avatar in await _profileRepository.GetLegacyAvatarsAsync(cancellationToken))
            {
                StoredFile photo = await _fileStorage.OpenAsync(avatar.BlobName, cancellationToken);
                await using (photo.Content)
                {
                    await _fileStorage.SetAvatarAsync(avatar.UserId, photo, true, cancellationToken);
                }
                await _profileRepository.ClearLegacyAvatarAsync(avatar, cancellationToken);
            }
        }

        private async Task<FighterProfile> RequireProfileAsync(Guid userId, CancellationToken cancellationToken)
        {
            FighterProfile? profile = await _profileRepository.GetAsync(userId, cancellationToken);
            if (profile == null)
            {
                throw new ApiException(404, "Profile not found.");
            }
            return profile;
        }
    }
}
