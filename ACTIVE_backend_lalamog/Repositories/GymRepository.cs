using Lalamog.Api.Data;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lalamog.Api.Repositories;

public class GymRepository : IGymRepository
{
    private readonly AppDbContext _db;
    public GymRepository(AppDbContext db)
    {
        _db = db;
    }
    public Task<List<Gym>> ListAsync(CancellationToken cancellationToken)
    {
        var query = from map in Graph().AsNoTracking() orderby map.Name select map;
        return query.ToListAsync(cancellationToken);
    }
    public Task<Gym?> GetAsync(long id, CancellationToken cancellationToken)
    {
        var query = from map in Graph().Include("Fights.Match").Include("Fights.Confirmations") where map.Id == id select map;
        return query.SingleOrDefaultAsync(cancellationToken);
    }
    public Task<bool> HasParticipantAsync(long gymId, Guid actor, CancellationToken cancellationToken)
    {
        var query = from fight in _db.Fights where fight.GymId == gymId
                    && (fight.Match.User1Id == actor || fight.Match.User2Id == actor) select fight;
        return query.AnyAsync(cancellationToken);
    }
    public Task<bool> HasUnfinishedAsync(long gymId, CancellationToken cancellationToken)
    {
        var query = from fight in _db.Fights where fight.GymId == gymId &&
            (fight.Status == "pending_approval" || fight.Status == "scheduled" || fight.Status == "confirmed") select fight;
        return query.AnyAsync(cancellationToken);
    }
    public Task<bool> HasOverlapAsync(long gymId, DateTimeOffset start, DateTimeOffset end, long excludeFightId, CancellationToken cancellationToken)
    {
        var query = from fight in _db.Fights where fight.GymId == gymId && fight.Id != excludeFightId
                    && (fight.Status == "scheduled" || fight.Status == "confirmed")
                    && fight.ScheduledAt < end && fight.ScheduledAt.AddMinutes(fight.SessionDurationMinutes ?? 60) > start select fight;
        return query.AnyAsync(cancellationToken);
    }
    public Task<List<Fight>> BookingsAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var query = from fight in _db.Fights.Include(nameof(Fight.Gym)).Include(nameof(Fight.Match)).Include(nameof(Fight.Confirmations))
                    where fight.Gym != null && fight.Gym.OwnerUserId == ownerId orderby fight.ScheduledAt descending select fight;
        return query.Take(100).ToListAsync(cancellationToken);
    }
    public async Task<string> FighterNameAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await (from profile in _db.Profiles where profile.UserId == userId select profile.FighterName).SingleOrDefaultAsync(cancellationToken) ?? "Fighter";
    }
    public Task<bool> CanManageGymsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return (from link in _db.UserRoles
                join grant in _db.RoleSystemFeatures on link.RoleId equals grant.RoleId
                where link.UserId == userId && grant.SystemFeature.Code == Lalamog.Api.Authorization.ApplicationPermissions.Owner_ManageGyms
                select link).AnyAsync(cancellationToken);
    }
    private IQueryable<Gym> Graph()
    {
        return _db.Gyms.Include("City.Province.Region").Include("City.Region").Include(nameof(Gym.Barangay));
    }
    public void Add(Gym map) { _db.Gyms.Add(map); }
    public Task<MapRegion?> GetRegionAsync(string code, CancellationToken cancellationToken)
    {
        var query = from item in _db.Regions where item.Code == code select item;
        return query.SingleOrDefaultAsync(cancellationToken);
    }
    public void Add(MapRegion value) { _db.Regions.Add(value); }
    public Task<MapProvince?> GetProvinceAsync(string code, CancellationToken cancellationToken)
    {
        var query = from item in _db.Provinces where item.Code == code select item;
        return query.SingleOrDefaultAsync(cancellationToken);
    }
    public void Add(MapProvince value) { _db.Provinces.Add(value); }
    public Task<MapCity?> GetCityAsync(string code, CancellationToken cancellationToken)
    {
        var query = from item in _db.Cities where item.Code == code select item;
        return query.SingleOrDefaultAsync(cancellationToken);
    }
    public void Add(MapCity value) { _db.Cities.Add(value); }
    public Task<MapBarangay?> GetBarangayAsync(string code, CancellationToken cancellationToken)
    {
        var query = from item in _db.Barangays where item.Code == code select item;
        return query.SingleOrDefaultAsync(cancellationToken);
    }
    public void Add(MapBarangay value) { _db.Barangays.Add(value); }
    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
