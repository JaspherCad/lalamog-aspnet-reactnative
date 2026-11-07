using Lalamog.Api.DTOs;

namespace Lalamog.Api.Interfaces;

public interface IAreaService
{
    string Version { get; }
    List<AreaOption> Regions();
    List<AreaOption> Provinces(string regionCode);
    List<AreaOption> Cities(string regionCode, string? provinceCode);
    List<AreaOption> Barangays(string cityCode);
    string CityName(string cityCode);
    List<Lalamog.Api.Models.PhilippineArea> GetPath(string code);
    void Validate(MapLocation location);
}
