using Lalamog.Api.DTOs;
using Lalamog.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lalamog.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/areas")]
[ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
public class AreasController : ControllerBase
{
    private readonly IAreaService _areas;

    public AreasController(IAreaService areas)
    {
        _areas = areas;
    }

    [HttpGet("regions")]
    public ActionResult<List<AreaOption>> Regions()
    {
        return Ok(_areas.Regions());
    }

    [HttpGet("provinces")]
    public ActionResult<List<AreaOption>> Provinces(string regionCode)
    {
        return Ok(_areas.Provinces(regionCode));
    }

    [HttpGet("cities")]
    public ActionResult<List<AreaOption>> Cities(string regionCode, string? provinceCode)
    {
        return Ok(_areas.Cities(regionCode, provinceCode));
    }

    [HttpGet("barangays")]
    public ActionResult<List<AreaOption>> Barangays(string cityCode)
    {
        return Ok(_areas.Barangays(cityCode));
    }
}
