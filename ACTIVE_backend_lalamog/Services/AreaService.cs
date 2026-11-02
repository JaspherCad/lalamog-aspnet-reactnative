using System.Text.Json;
using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;

namespace Lalamog.Api.Services;

public class AreaService : IAreaService
{
    private readonly Dictionary<string, PhilippineArea> _areas = new(StringComparer.Ordinal);
    public string Version { get; }

    public AreaService(IWebHostEnvironment environment)
    {
        string path = Path.Combine(environment.ContentRootPath, "Data", "Reference", "psgc-2026-q3.json");
        AreaCatalog catalog = JsonSerializer.Deserialize<AreaCatalog>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("The bundled Philippine area catalog is missing.");
        Version = catalog.Version;
        foreach (PhilippineArea area in catalog.Areas)
        {
            _areas.Add(area.Code, area);
        }
    }

    public List<AreaOption> Regions()
    {
        var query = from area in _areas.Values
                    where area.Level == "region"
                    orderby area.Name
                    select new AreaOption { Code = area.Code, Name = area.Name };
        return query.ToList();
    }

    public List<AreaOption> Provinces(string regionCode)
    {
        Require(regionCode, "region");
        var query = from area in _areas.Values
                    where area.Level == "province" && area.ParentCode == regionCode
                    orderby area.Name
                    select new AreaOption { Code = area.Code, Name = area.Name };
        return query.ToList();
    }

    public List<AreaOption> Cities(string regionCode, string? provinceCode)
    {
        Require(regionCode, "region");
        if (provinceCode != null && provinceCode != "none")
        {
            PhilippineArea province = Require(provinceCode, "province");
            if (province.ParentCode != regionCode)
            {
                throw new ApiException(400, "Choose a province in the selected region.");
            }
        }
        var query = from area in _areas.Values
                    where area.Level == "city" && BelongsTo(area, regionCode)
                    where provinceCode == null || area.ParentCode == provinceCode
                        || (provinceCode == "none" && area.ParentCode == regionCode)
                    orderby area.Name
                    select new AreaOption { Code = area.Code, Name = area.Name };
        return query.ToList();
    }

    public List<AreaOption> Barangays(string cityCode)
    {
        Require(cityCode, "city");
        var query = from area in _areas.Values
                    where area.Level == "barangay" && BelongsTo(area, cityCode)
                    orderby area.Name
                    select new AreaOption { Code = area.Code, Name = area.Name };
        return query.ToList();
    }

    public string CityName(string cityCode)
    {
        return Require(cityCode, "city").Name;
    }

    public void Validate(MapLocation location)
    {
        if (location.Latitude.HasValue != location.Longitude.HasValue)
        {
            throw new ApiException(400, "Supply both latitude and longitude, or clear both.");
        }
        if (location.Latitude.HasValue && (!double.IsFinite(location.Latitude.Value)
            || !double.IsFinite(location.Longitude!.Value) || location.Latitude < -90 || location.Latitude > 90
            || location.Longitude < -180 || location.Longitude > 180))
        {
            throw new ApiException(400, "Choose valid latitude (-90 to 90) and longitude (-180 to 180).");
        }
        if (location.RegionCode == null && location.ProvinceCode == null
            && location.CityCode == null && location.BarangayCode == null)
        {
            return;
        }
        if (location.PsgcVersion != null && location.PsgcVersion != Version)
        {
            throw new ApiException(400, "Reload the area catalog before saving this location.");
        }
        PhilippineArea region = Require(location.RegionCode, "region");
        PhilippineArea city = Require(location.CityCode, "city");
        PhilippineArea barangay = Require(location.BarangayCode, "barangay");
        if (!BelongsTo(city, region.Code) || !BelongsTo(barangay, city.Code))
        {
            throw new ApiException(400, "The region, city and barangay must belong to the same area hierarchy.");
        }
        string? expectedProvince = null;
        if (city.ParentCode != null && _areas[city.ParentCode].Level == "province")
        {
            expectedProvince = city.ParentCode;
        }
        if (location.ProvinceCode != expectedProvince)
        {
            throw new ApiException(400, "Choose the city's province, or independent city for NCR/HUC/ICC areas.");
        }
    }

    private PhilippineArea Require(string? code, string level)
    {
        if (code == null || !_areas.TryGetValue(code, out PhilippineArea? area) || area.Level != level)
        {
            throw new ApiException(400, "Choose a valid " + level + " from the Philippine area catalog.");
        }
        return area;
    }

    private bool BelongsTo(PhilippineArea area, string ancestor)
    {
        while (area.ParentCode != null)
        {
            if (area.ParentCode == ancestor)
            {
                return true;
            }
            area = _areas[area.ParentCode];
        }
        return false;
    }
}
