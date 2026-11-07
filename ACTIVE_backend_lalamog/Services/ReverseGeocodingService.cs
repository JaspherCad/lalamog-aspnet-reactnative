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

        cancellationToken.ThrowIfCancellationRequested();
        using HttpClient client = _clients.CreateClient("AzureMaps");
        if (!client.DefaultRequestHeaders.Contains("subscription-key"))
        {
            throw new ApiException(503, "Azure Maps address lookup is not configured. Enter the address manually.");
        }
        string lat = latitude.ToString("R", CultureInfo.InvariantCulture);
        string lon = longitude.ToString("R", CultureInfo.InvariantCulture);
        string key = "reverse:azuremaps:2026-01-01:" + client.BaseAddress + lon + "," + lat;
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
                "reverseGeocode?api-version=2026-01-01&coordinates=" + lon + "," + lat, cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests
                || (response.StatusCode == HttpStatusCode.ServiceUnavailable && response.Headers.RetryAfter != null))
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
                if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("features", out JsonElement features)
                    || features.ValueKind != JsonValueKind.Array)
                {
                    throw new ApiException(503, "Address lookup returned no usable data. Enter the address manually.");
                }
                foreach (JsonElement feature in features.EnumerateArray())
                {
                    result.Features.Add(feature.Clone());
                    if (result.Address.Length == 0)
                    {
                        ReadAddress(feature, result);
                    }
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

    private static void ReadAddress(JsonElement feature, ReverseGeocodeResponse result)
    {
        if (feature.ValueKind != JsonValueKind.Object || !feature.TryGetProperty("properties", out JsonElement properties)
            || properties.ValueKind != JsonValueKind.Object || !properties.TryGetProperty("address", out JsonElement address)
            || address.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        result.Road = Text(address, "streetName");
        result.StreetNumber = Text(address, "streetNumber");
        result.Suburb = Text(address, "neighborhood");
        result.City = Text(address, "locality");
        result.PostalCode = Text(address, "postalCode");
        var parts = new List<string>();
        string street = string.Join(" ", new[] { result.StreetNumber, result.Road }).Trim();
        AddPart(parts, Text(address, "addressLine") ?? street);
        AddPart(parts, result.Suburb);
        AddPart(parts, result.City);
        result.State = null;
        if (address.TryGetProperty("adminDistricts", out JsonElement districts) && districts.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement district in districts.EnumerateArray())
            {
                string? name = Text(district, "name");
                result.State ??= name;
                AddPart(parts, name);
            }
        }
        AddPart(parts, result.PostalCode);
        result.Country = null;
        result.CountryCode = null;
        if (address.TryGetProperty("countryRegion", out JsonElement country) && country.ValueKind == JsonValueKind.Object)
        {
            result.Country = Text(country, "name");
            result.CountryCode = Text(country, "ISO");
        }
        AddPart(parts, result.Country);
        result.Address = Text(address, "formattedAddress") ?? string.Join(", ", parts);
        result.ResultType = Text(properties, "type");
        result.Confidence = Text(properties, "confidence");
        result.MatchCodes.Clear();
        if (properties.TryGetProperty("matchCodes", out JsonElement codes) && codes.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement code in codes.EnumerateArray())
            {
                if (code.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(code.GetString()))
                {
                    result.MatchCodes.Add(code.GetString()!);
                }
            }
        }
    }

    private static void AddPart(List<string> parts, string? part)
    {
        if (!string.IsNullOrWhiteSpace(part) && !parts.Contains(part))
        {
            parts.Add(part);
        }
    }

    private static string? Text(JsonElement value, string property)
    {
        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty(property, out JsonElement text) && text.ValueKind == JsonValueKind.String)
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
