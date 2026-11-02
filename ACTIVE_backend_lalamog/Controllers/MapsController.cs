using Lalamog.Api.DTOs;
using Lalamog.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lalamog.Api.Controllers;

[Route("api/maps")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class MapsController : ApiController
{
    private readonly IReverseGeocodingService _geocoding;

    public MapsController(IReverseGeocodingService geocoding)
    {
        _geocoding = geocoding;
    }

    [HttpPost("reverse")]
    public async Task<ActionResult<ReverseGeocodeResponse>> Reverse(ReverseGeocodeRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _geocoding.ReverseAsync(request.Latitude!.Value, request.Longitude!.Value, cancellationToken));
    }
}
