using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
namespace Lalamog.Api.Services;

public class GymService : IGymService
{
    private readonly IGymRepository _gyms;
    private readonly IAreaService _areas;
    private readonly IMatchingRepository _transactions;
    private readonly IFightRepository _fights;
    private readonly IFileStorage _storage;
    private readonly TimeProvider _clock;
    public GymService(IGymRepository gyms, IAreaService areas, IMatchingRepository transactions, IFightRepository fights, IFileStorage storage, TimeProvider clock)
    {
        _gyms = gyms; _areas = areas; _transactions = transactions; _fights = fights; _storage = storage; _clock = clock;
    }
    public async Task<List<GymResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var result = new List<GymResponse>();
        foreach (Gym gym in await _gyms.ListAsync(cancellationToken))
        {
            if (gym.IsPublished && gym.OwnerUserId != null && await _gyms.CanManageGymsAsync(gym.OwnerUserId.Value, cancellationToken)) { result.Add(await ToResponseAsync(gym, false, cancellationToken)); }
        }
        return result;
    }
    public async Task<GymResponse> GetAsync(long id, Guid? actor, CancellationToken cancellationToken)
    {
        Gym gym = await _gyms.GetAsync(id, cancellationToken) ?? throw new ApiException(404, "Gym not found.");
        bool visible = gym.IsPublished && gym.OwnerUserId != null && await _gyms.CanManageGymsAsync(gym.OwnerUserId.Value, cancellationToken);
        if (!visible && (actor == null || (gym.OwnerUserId != actor && !await _gyms.HasParticipantAsync(id, actor.Value, cancellationToken))))
        { throw new ApiException(404, "Gym not found."); }
        return await ToResponseAsync(gym, true, cancellationToken);
    }
    public async Task<List<GymResponse>> OwnedAsync(Guid actor, CancellationToken cancellationToken)
    {
        var result = new List<GymResponse>();
        foreach (Gym gym in await _gyms.ListAsync(cancellationToken))
        {
            if (gym.OwnerUserId == actor) { result.Add(await ToResponseAsync(gym, false, cancellationToken)); }
        }
        return result;
    }
    public Task<GymResponse> CreateAsync(Guid actor, GymRequest request, CancellationToken cancellationToken)
    { return SaveAsync(actor, null, request, cancellationToken); }
    public Task<GymResponse> UpdateAsync(Guid actor, long id, GymRequest request, CancellationToken cancellationToken)
    { return SaveAsync(actor, id, request, cancellationToken); }
    private async Task<GymResponse> SaveAsync(Guid actor, long? id, GymRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Address)) { throw new ApiException(400, "Enter the gym name and public address."); }
        if (!request.Latitude.HasValue || !request.Longitude.HasValue) { throw new ApiException(400, "Pin your gym location."); }
        if (decimal.Round(request.SessionPrice, 2) != request.SessionPrice) { throw new ApiException(400, "Use at most two decimal places for the session price."); }
        _areas.Validate(new MapLocation { Latitude = request.Latitude, Longitude = request.Longitude });
        List<PhilippineArea> path = _areas.GetPath(request.AreaCode);
        if (path[^1].Level != "city" && path[^1].Level != "barangay") { throw new ApiException(400, "Choose a city or barangay."); }
        async Task<GymResponse> SaveInTransactionAsync()
        {
            Gym? gym = null;
            if (id != null)
            {
                gym = await RequireOwnerAsync(actor, id.Value, cancellationToken);
                if (request.Version != Convert.ToBase64String(gym.Version)) { throw new ApiException(409, "Gym details changed. Refresh before saving."); }
                bool changedLocation = gym.Address != request.Address.Trim() || gym.Latitude != request.Latitude || gym.Longitude != request.Longitude
                    || (gym.Barangay?.Code ?? gym.City.Code) != request.AreaCode;
                if (changedLocation && await _gyms.HasUnfinishedAsync(gym.Id, cancellationToken)) { throw new ApiException(409, "Finish or cancel existing bookings before changing the gym location."); }
            }
            MapRegion? region = null;
            MapProvince? province = null;
            MapCity? city = null;
            MapBarangay? barangay = null;
            foreach (PhilippineArea item in path)
            {
                switch (item.Level)
                {
                    case "region":
                        region = await _gyms.GetRegionAsync(item.Code, cancellationToken);
                        if (region == null) { region = new MapRegion { Code = item.Code, Name = item.Name }; _gyms.Add(region); }
                        break;
                    case "province":
                        province = await _gyms.GetProvinceAsync(item.Code, cancellationToken);
                        if (province == null) { province = new MapProvince { Code = item.Code, Name = item.Name, Region = region! }; _gyms.Add(province); }
                        break;
                    case "city":
                        city = await _gyms.GetCityAsync(item.Code, cancellationToken);
                        if (city == null) { city = new MapCity { Code = item.Code, Name = item.Name, Province = province, Region = province == null ? region : null }; _gyms.Add(city); }
                        break;
                    case "barangay":
                        barangay = await _gyms.GetBarangayAsync(item.Code, cancellationToken);
                        if (barangay == null) { barangay = new MapBarangay { Code = item.Code, Name = item.Name, City = city! }; _gyms.Add(barangay); }
                        break;
                }
                await _gyms.SaveAsync(cancellationToken);
            }
            if (gym == null)
            {
                gym = new Gym { Slug = request.Slug, OwnerUserId = actor };
                _gyms.Add(gym);
            }
            gym.Name = request.Name.Trim(); gym.Description = request.Description?.Trim() ?? ""; gym.ContactInformation = request.ContactInformation?.Trim() ?? "";
            gym.Address = request.Address.Trim(); gym.Latitude = request.Latitude!.Value; gym.Longitude = request.Longitude!.Value;
            gym.City = city!; gym.CityId = city!.Id; gym.Barangay = barangay; gym.BarangayId = barangay?.Id;
            gym.SessionPrice = request.SessionPrice; gym.SessionDurationMinutes = request.SessionDurationMinutes;
            await _gyms.SaveAsync(cancellationToken);
            return await ToResponseAsync(gym, false, cancellationToken);
        }
        return await _transactions.InTransactionAsync(SaveInTransactionAsync, cancellationToken);
    }
    private async Task<Gym> RequireOwnerAsync(Guid actor, long id, CancellationToken cancellationToken)
    {
        Gym gym = await _gyms.GetAsync(id, cancellationToken) ?? throw new ApiException(404, "Gym not found.");
        if (gym.OwnerUserId != actor) { throw new ApiException(404, "Gym not found."); }
        return gym;
    }
    public async Task<GymResponse> PublishAsync(Guid actor, long id, bool publish, CancellationToken cancellationToken)
    {
        Gym gym = await RequireOwnerAsync(actor, id, cancellationToken);
        if (publish && !await _gyms.CanManageGymsAsync(actor, cancellationToken)) { throw new ApiException(403, "Gym management permission is required."); }
        gym.IsPublished = publish;
        await _gyms.SaveAsync(cancellationToken);
        return await ToResponseAsync(gym, false, cancellationToken);
    }
    public async Task<List<GymBookingResponse>> BookingsAsync(Guid actor, CancellationToken cancellationToken)
    {
        var result = new List<GymBookingResponse>();
        foreach (Fight fight in await _gyms.BookingsAsync(actor, cancellationToken)) { result.Add(await ToBookingAsync(fight, cancellationToken)); }
        return result;
    }
    public async Task<GymBookingResponse> DecideAsync(Guid actor, long fightId, string action, string? reason, CancellationToken cancellationToken)
    {
        async Task<GymBookingResponse> DecideInTransactionAsync()
        {
            Fight fight = await _fights.GetAsync(fightId, cancellationToken) ?? throw new ApiException(404, "Booking not found.");
            if (fight.Gym == null || fight.Gym.OwnerUserId != actor) { throw new ApiException(404, "Booking not found."); }
            if (action == "approve")
            {
                if (fight.Status != "pending_approval" || fight.ScheduledAt <= _clock.GetUtcNow()) { throw new ApiException(409, "This request cannot be approved. Cancel expired requests."); }
                DateTimeOffset end = fight.ScheduledAt.AddMinutes(fight.SessionDurationMinutes ?? 60);
                if (await _gyms.HasOverlapAsync(fight.GymId!.Value, fight.ScheduledAt, end, fight.Id, cancellationToken)) { throw new ApiException(409, "The gym already has an approved booking during this session."); }
                fight.Status = "scheduled";
            }
            else if (action == "complete")
            {
                if (fight.Status != "confirmed" || fight.Confirmations.Count != 2 || fight.ScheduledAt.AddMinutes(fight.SessionDurationMinutes ?? 60) > _clock.GetUtcNow())
                { throw new ApiException(409, "Both waivers and the end of the booked session are required."); }
                fight.Status = "completed";
            }
            else
            {
                if (action != "decline" && action != "cancel") { throw new ApiException(400, "Unknown booking action."); }
                if ((action == "decline" && fight.Status != "pending_approval") || (fight.Status != "pending_approval" && fight.Status != "scheduled" && fight.Status != "confirmed"))
                { throw new ApiException(409, "This booking cannot be changed."); }
                if (string.IsNullOrWhiteSpace(reason)) { throw new ApiException(400, "Enter a reason."); }
                fight.Status = "canceled"; fight.CancellationReason = reason.Trim();
            }
            fight.UpdatedAt = _clock.GetUtcNow();
            await _fights.SaveAsync(cancellationToken);
            return await ToBookingAsync(fight, cancellationToken);
        }
        return await _transactions.InTransactionAsync(DecideInTransactionAsync, cancellationToken);
    }
    private async Task<GymBookingResponse> ToBookingAsync(Fight fight, CancellationToken cancellationToken)
    {
        return new GymBookingResponse { Id = fight.Id, GymId = fight.GymId!.Value, GymName = fight.Gym!.Name,
            Fighter1Name = await _gyms.FighterNameAsync(fight.Match.User1Id, cancellationToken), Fighter2Name = await _gyms.FighterNameAsync(fight.Match.User2Id, cancellationToken),
            ScheduledAt = fight.ScheduledAt, Status = fight.Status, QuotedPrice = fight.QuotedPrice, SessionDurationMinutes = fight.SessionDurationMinutes,
            ConfirmationCount = fight.Confirmations.Count, CancellationReason = fight.CancellationReason };
    }
    private async Task<GymResponse> ToResponseAsync(Gym gym, bool history, CancellationToken cancellationToken)
    {
        var response = new GymResponse { Id = gym.Id, Slug = gym.Slug, Name = gym.Name, Description = gym.Description, ContactInformation = gym.ContactInformation,
            Address = gym.Address, Latitude = gym.Latitude, Longitude = gym.Longitude, IsDemo = gym.IsDemo, IsPublished = gym.IsPublished,
            SessionPrice = gym.SessionPrice, SessionDurationMinutes = gym.SessionDurationMinutes, Version = Convert.ToBase64String(gym.Version), CoverUrl = $"/api/gyms/{gym.Id}/cover" };
        MapCity city = gym.City; MapRegion region = city.Province?.Region ?? city.Region!;
        response.AreaPath.Add(new MapAreaResponse { Code = region.Code, Name = region.Name, Level = "region" });
        if (city.Province != null) { response.AreaPath.Add(new MapAreaResponse { Code = city.Province.Code, Name = city.Province.Name, Level = "province" }); }
        response.AreaPath.Add(new MapAreaResponse { Code = city.Code, Name = city.Name, Level = "city" });
        if (gym.Barangay != null) { response.AreaPath.Add(new MapAreaResponse { Code = gym.Barangay.Code, Name = gym.Barangay.Name, Level = "barangay" }); }
        if (history)
        {
            foreach (Fight fight in (from fight in gym.Fights where fight.Status == "completed" orderby fight.ScheduledAt descending select fight).Take(100))
            {
                var item = new PublicFightResponse { Id = fight.Id, ScheduledAt = fight.ScheduledAt, Status = fight.Status };
                if (fight.Confirmations.Count == 2 && (from confirmation in fight.Confirmations where confirmation.AllowPublicIdentity select confirmation).Count() == 2)
                {
                    item.Fighter1Name = await _gyms.FighterNameAsync(fight.Match.User1Id, cancellationToken);
                    item.Fighter2Name = await _gyms.FighterNameAsync(fight.Match.User2Id, cancellationToken);
                    item.Fighter1AvatarUrl = $"/api/media/avatars/{fight.Match.User1Id:D}";
                    item.Fighter2AvatarUrl = $"/api/media/avatars/{fight.Match.User2Id:D}";
                }
                response.Fights.Add(item);
            }
        }
        return response;
    }
    public async Task UploadCoverAsync(Guid actor, long id, IFormFile file, CancellationToken cancellationToken)
    {
        await RequireOwnerAsync(actor, id, cancellationToken);
        string temporary = await _storage.SavePhotoAsync(file, cancellationToken);
        try
        {
            StoredFile photo = await _storage.OpenAsync(temporary, cancellationToken);
            await using (photo.Content) { await _storage.SetGymCoverAsync(id, photo, cancellationToken); }
        }
        finally { await _storage.DeleteAsync(temporary, cancellationToken); }
    }
    public async Task<StoredFile> CoverAsync(long id, Guid? actor, CancellationToken cancellationToken)
    {
        await GetAsync(id, actor, cancellationToken);
        return await _storage.OpenAsync($"gyms/{id}/cover", cancellationToken);
    }
    public async Task<List<AdminGymResponse>> AdminListAsync(CancellationToken cancellationToken)
    {
        var result = new List<AdminGymResponse>();
        foreach (Gym gym in await _gyms.ListAsync(cancellationToken))
        { result.Add(new AdminGymResponse { Gym = await ToResponseAsync(gym, false, cancellationToken), OwnerUserId = gym.OwnerUserId }); }
        return result;
    }
    public async Task AdminUnpublishAsync(long id, CancellationToken cancellationToken)
    {
        Gym gym = await _gyms.GetAsync(id, cancellationToken) ?? throw new ApiException(404, "Gym not found.");
        gym.IsPublished = false; await _gyms.SaveAsync(cancellationToken);
    }
    public async Task AssignOwnerAsync(long id, Guid ownerId, CancellationToken cancellationToken)
    {
        Gym gym = await _gyms.GetAsync(id, cancellationToken) ?? throw new ApiException(404, "Gym not found.");
        if (gym.OwnerUserId != null) { throw new ApiException(409, "This gym already has an owner."); }
        if (!await _gyms.CanManageGymsAsync(ownerId, cancellationToken)) { throw new ApiException(400, "Choose an account with gym management permission."); }
        gym.OwnerUserId = ownerId; await _gyms.SaveAsync(cancellationToken);
    }
}
