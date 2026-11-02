using Lalamog.Api.DTOs;

namespace Lalamog.Api.Interfaces;

public interface IReverseGeocodingService
{
    Task<ReverseGeocodeResponse> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken);
}
