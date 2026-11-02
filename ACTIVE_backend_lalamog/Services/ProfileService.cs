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
            string key = await _fileStorage.SavePhotoAsync(file, cancellationToken);

            // Existing tabs may still reference an older immutable photo URL.
            // Reference-aware cleanup remains a separate maintenance workflow.
            profile.AvatarKey = key;
            try
            {
                await _profileRepository.SaveAsync(cancellationToken);
            }
            catch
            {
                await _fileStorage.DeleteAsync(key, CancellationToken.None);
                throw;
            }

            return DtoMapper.ToProfileResponse(profile, true);
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
