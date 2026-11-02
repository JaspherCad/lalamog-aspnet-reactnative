using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Lalamog.Api.Services;

public class ReverseGeocodingService : IReverseGeocodingService, IDisposable
{
    private readonly IHttpClientFactory _clients;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _minimumInterval;
    private readonly TimeSpan _cacheDuration;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private long _lastRequest;
    private DateTimeOffset _retryAfter;

    public ReverseGeocodingService(IHttpClientFactory clients, IMemoryCache cache, TimeSpan minimumInterval, TimeSpan cacheDuration)
    {
        _clients = clients;
        _cache = cache;
        _minimumInterval = minimumInterval;
        _cacheDuration = cacheDuration;
    }

    public async Task<ReverseGeocodeResponse> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        if (!double.IsFinite(latitude) || !double.IsFinite(longitude) || Math.Abs(latitude) > 90 || Math.Abs(longitude) > 180)
        {
            throw new ApiException(400, "Choose a valid map pin first.");
        }

        using HttpClient client = _clients.CreateClient("Nominatim");
        string lat = latitude.ToString("F5", CultureInfo.InvariantCulture);
        string lon = longitude.ToString("F5", CultureInfo.InvariantCulture);
        string key = "reverse:" + client.BaseAddress + lat + "," + lon;
        if (_cache.TryGetValue(key, out ReverseGeocodeResponse? cached))
        {
            return RequireAddress(cached!);
        }

        // One shared gate across all users; recheck cache to deduplicate waiting lookups.
        if (!await _gate.WaitAsync(client.Timeout, cancellationToken))
        {
            throw new ApiException(429, "Address lookup is busy. Try again shortly or enter the address manually.");
        }
        try
        {
            if (_cache.TryGetValue(key, out cached))
            {
                return RequireAddress(cached!);
            }
            if (_retryAfter > DateTimeOffset.UtcNow)
            {
                throw new ApiException(503, "Address lookup is busy. Try again later or enter the address manually.");
            }
            if (_lastRequest != 0)
            {
                TimeSpan delay = _minimumInterval - Stopwatch.GetElapsedTime(_lastRequest);
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken);
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            _lastRequest = Stopwatch.GetTimestamp();
            using HttpResponseMessage response = await client.GetAsync(
                "reverse?format=jsonv2&lat=" + lat + "&lon=" + lon + "&zoom=18&addressdetails=1&accept-language=en", cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests || response.StatusCode == HttpStatusCode.Forbidden)
            {
                _retryAfter = response.Headers.RetryAfter?.Date
                    ?? DateTimeOffset.UtcNow.Add(response.Headers.RetryAfter?.Delta ?? _minimumInterval);
                throw new ApiException(503, "Address lookup is busy. Try again later or enter the address manually.");
            }

            var result = new ReverseGeocodeResponse();
            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                response.EnsureSuccessStatusCode();
                using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                JsonElement root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("address", out JsonElement address)
                    && address.ValueKind == JsonValueKind.Object)
                {
                    result.Road = Text(address, "road");
                    result.Suburb = Text(address, "suburb") ?? Text(address, "neighbourhood") ?? Text(address, "quarter");
                    result.City = Text(address, "city") ?? Text(address, "town") ?? Text(address, "village");
                    result.State = Text(address, "state");
                    result.Country = Text(address, "country");
                    var parts = new List<string>();
                    foreach (string? part in new[] { result.Road, result.Suburb, result.City, result.State, result.Country })
                    {
                        if (!string.IsNullOrWhiteSpace(part) && !parts.Contains(part))
                        {
                            parts.Add(part);
                        }
                    }
                    result.Address = string.Join(", ", parts);
                }
                if (result.Address.Length == 0 && root.ValueKind == JsonValueKind.Object)
                {
                    result.Address = Text(root, "display_name") ?? string.Empty;
                }
            }
            _cache.Set(key, result, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = _cacheDuration, Size = 1 });
            return RequireAddress(result);
        }
        catch (HttpRequestException)
        {
            throw new ApiException(503, "Address lookup is unavailable. You can still enter the address manually.");
        }
        catch (JsonException)
        {
            throw new ApiException(503, "Address lookup returned no usable data. Enter the address manually.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiException(503, "Address lookup timed out. You can still enter the address manually.");
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string? Text(JsonElement value, string property)
    {
        if (value.TryGetProperty(property, out JsonElement text) && text.ValueKind == JsonValueKind.String)
        {
            string? result = text.GetString()?.Trim();
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
        }
        return null;
    }

    private static ReverseGeocodeResponse RequireAddress(ReverseGeocodeResponse result)
    {
        if (result.Address.Length == 0)
        {
            throw new ApiException(404, "No address was found for this pin. Enter the address manually.");
        }
        return result;
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}
