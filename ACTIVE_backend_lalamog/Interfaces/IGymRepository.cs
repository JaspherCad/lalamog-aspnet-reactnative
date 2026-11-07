using Lalamog.Api.Models;

namespace Lalamog.Api.Interfaces;

public interface IGymRepository
{
    Task<bool> HasParticipantAsync(long gymId, Guid actor, CancellationToken cancellationToken);
    Task<bool> HasUnfinishedAsync(long gymId, CancellationToken cancellationToken);
    Task<bool> HasOverlapAsync(long gymId, DateTimeOffset start, DateTimeOffset end, long excludeFightId, CancellationToken cancellationToken);
    Task<List<Fight>> BookingsAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<string> FighterNameAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> CanManageGymsAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<Gym>> ListAsync(CancellationToken cancellationToken);
    Task<Gym?> GetAsync(long id, CancellationToken cancellationToken);
    Task<MapRegion?> GetRegionAsync(string code, CancellationToken cancellationToken);
    void Add(MapRegion value);
    Task<MapProvince?> GetProvinceAsync(string code, CancellationToken cancellationToken);
    void Add(MapProvince value);
    Task<MapCity?> GetCityAsync(string code, CancellationToken cancellationToken);
    void Add(MapCity value);
    Task<MapBarangay?> GetBarangayAsync(string code, CancellationToken cancellationToken);
    void Add(MapBarangay value);
    void Add(Gym map);
    Task SaveAsync(CancellationToken cancellationToken);
}
