using Lalamog.Api.Contracts;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
namespace Lalamog.Api.Services;
public class ProfileService(IProfileRepository profiles, IFileStorage files)
{
    public static ProfileResponse ToResponse(FighterProfile p) => new(p.UserId, p.Nickname,
        p.Bio, p.FightingStyle, p.ExperienceLevel, p.Location, p.Availability,
        p.AvatarKey is null ? null : $"/api/media/{p.AvatarKey}");
    public async Task<ProfileResponse> GetAsync(Guid userId, CancellationToken ct) =>
        ToResponse(await RequiredAsync(userId, ct));
    private async Task<FighterProfile> RequiredAsync(Guid userId, CancellationToken ct) =>
        await profiles.GetAsync(userId, ct) ?? throw new ApiException(404, "Profile not found.");
    public async Task<List<ProfileResponse>> DiscoverAsync(Guid userId, CancellationToken ct) =>
        (await profiles.DiscoverAsync(userId, ct)).Select(ToResponse).ToList();
    public async Task<ProfileResponse> UpdateAsync(Guid userId, ProfileRequest request, CancellationToken ct)
    {
        if (request.Nickname.Trim().Length < 2 || string.IsNullOrWhiteSpace(request.FightingStyle))
            throw new ApiException(400, "Enter a nickname and fighting style.");
        var profile = await RequiredAsync(userId, ct);
        profile.Nickname = request.Nickname.Trim();
        profile.Bio = (request.Bio ?? "").Trim();
        profile.FightingStyle = request.FightingStyle.Trim();
        profile.ExperienceLevel = request.ExperienceLevel;
        profile.Location = (request.Location ?? "").Trim();
        profile.Availability = (request.Availability ?? "").Trim();
        await profiles.SaveAsync(ct);
        return ToResponse(profile);
    }
    public async Task<ProfileResponse> AvatarAsync(Guid userId, IFormFile file, CancellationToken ct)
    {
        var profile = await RequiredAsync(userId, ct);
        var key = await files.SavePhotoAsync(file, ct);
        // Keep old keys valid: browser tabs can still reference the previous photo.
        // Unreferenced-photo cleanup is intentionally deferred to a maintenance workflow.
        profile.AvatarKey = key;
        try { await profiles.SaveAsync(ct); }
        catch { await files.DeleteAsync(key, CancellationToken.None); throw; }
        return ToResponse(profile);
    }
}