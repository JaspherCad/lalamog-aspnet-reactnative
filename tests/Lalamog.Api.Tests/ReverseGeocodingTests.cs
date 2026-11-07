using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Lalamog.Api.Tests;

public class ReverseGeocodingTests
{
    internal const string DetailedResult = """
        {"type":"FeatureCollection","features":[{
          "type":"Feature","id":"fictional-test-address","bbox":[121.03,14.63,121.05,14.65],
          "geometry":{"type":"Point","coordinates":[121.0401,14.6401]},
          "properties":{
            "address":{"formattedAddress":"42 East Avenue, Pinyahan, Quezon City 1100, Philippines",
              "addressLine":"42 East Avenue","streetName":"East Avenue","streetNumber":"42",
              "neighborhood":"Pinyahan","locality":"Quezon City","postalCode":"1100",
              "adminDistricts":[{"name":"Metro Manila","shortName":"NCR"},{"name":"Test district"}],
              "countryRegion":{"name":"Philippines","ISO":"PH"},"extraAddressField":"retained"},
            "type":"Address","confidence":"High","matchCodes":["Good"],
            "geocodePoints":[{"geometry":{"type":"Point","coordinates":[121.0402,14.6402]},
              "calculationMethod":"Rooftop","usageTypes":["Route"]}],"extraProviderField":"retained"
          }
        },{"type":"Feature","properties":{"type":"Neighborhood","address":{"formattedAddress":"Pinyahan"}}}]}
        """;

    [Fact]
    public async Task ConcurrentIdenticalPinsShareAzureLookup_AndKeepCompleteDetails()
    {
        using var handler = new ProviderHandler(DetailedResult);
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        using var service = Create(handler, cache);
        var calls = await Task.WhenAll(service.ReverseAsync(14.640001, 121.040001, default), service.ReverseAsync(14.640001, 121.040001, default));
        Assert.Equal(1, handler.Calls);
        Assert.All(calls, result => Assert.Equal("42 East Avenue, Pinyahan, Quezon City 1100, Philippines", result.Address));
        Assert.Equal("/reverseGeocode", handler.Uri!.AbsolutePath);
        Assert.Contains("coordinates=121.040001,14.640001", handler.Uri.Query);
        Assert.Contains("api-version=2026-01-01", handler.Uri.Query);
        Assert.DoesNotContain("test-key", handler.Uri.Query);
        Assert.Equal("test-key", handler.Key);
        Assert.Equal("Pinyahan", calls[0].Suburb);
        var result = calls[0];
        Assert.Equal("Azure Maps", result.Provider);
        Assert.Equal("42", result.StreetNumber);
        Assert.Equal("1100", result.PostalCode);
        Assert.Equal("PH", result.CountryCode);
        Assert.Equal("Metro Manila", result.State);
        Assert.Equal("Address", result.ResultType);
        Assert.Equal("High", result.Confidence);
        Assert.Equal(new[] { "Good" }, result.MatchCodes);
        Assert.Equal(2, result.Features.Count);
        // Cloned provider data remains readable after its JsonDocument is disposed.
        JsonElement feature = result.Features[0];
        Assert.Equal("fictional-test-address", feature.GetProperty("id").GetString());
        Assert.Equal(4, feature.GetProperty("bbox").GetArrayLength());
        JsonElement properties = feature.GetProperty("properties");
        Assert.Equal(2, properties.GetProperty("address").GetProperty("adminDistricts").GetArrayLength());
        Assert.Equal("retained", properties.GetProperty("address").GetProperty("extraAddressField").GetString());
        Assert.Equal("retained", properties.GetProperty("extraProviderField").GetString());
        Assert.Equal("Rooftop", properties.GetProperty("geocodePoints")[0].GetProperty("calculationMethod").GetString());
        Assert.Contains("extraProviderField", JsonSerializer.Serialize(result));
    }

    [Theory]
    [InlineData("{\"locality\":\"Example Town\",\"countryRegion\":{\"name\":\"Philippines\"}}", "Example Town, Philippines")]
    [InlineData("{\"streetNumber\":\"10\",\"streetName\":\"Example Road\",\"postalCode\":\"1000\"}", "10 Example Road, 1000")]
    [InlineData("{\"formattedAddress\":\"Available map label\"}", "Available map label")]
    public async Task MissingComponentsUseAvailableLabels(string address, string expected)
    {
        using var handler = new ProviderHandler("{\"features\":[{\"properties\":{\"address\":" + address + "}}]}");
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        using var service = Create(handler, cache);
        Assert.Equal(expected, (await service.ReverseAsync(0, 0, default)).Address);
    }

    [Fact]
    public async Task NearbyDistinctPinsKeepFullPrecision_AndArePacedAcrossUsers()
    {
        using var handler = new ProviderHandler(DetailedResult);
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        using var service = Create(handler, cache);
        await Task.WhenAll(service.ReverseAsync(14.640001, 121.040001, default), service.ReverseAsync(14.640002, 121.040002, default));
        Assert.Equal(2, handler.Calls);
        Assert.True(Stopwatch.GetElapsedTime(handler.Starts[0], handler.Starts[1]) >= TimeSpan.FromMilliseconds(990));
    }

    [Fact]
    public async Task ProviderRateLimitHonorsRetryAfter_WithoutAnotherExternalRequest()
    {
        using var handler = new ProviderHandler("{}", HttpStatusCode.TooManyRequests);
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        using var service = Create(handler, cache);
        Assert.Equal(503, (await Assert.ThrowsAsync<ApiException>(() => service.ReverseAsync(14.64, 121.04, default))).StatusCode);
        Assert.Equal(503, (await Assert.ThrowsAsync<ApiException>(() => service.ReverseAsync(14.65, 121.05, default))).StatusCode);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData("none", 404)]
    [InlineData("empty", 404)]
    [InlineData("bad-json", 503)]
    [InlineData("bad-shape", 503)]
    [InlineData("offline", 503)]
    [InlineData("timeout", 503)]
    [InlineData("unauthorized", 503)]
    [InlineData("forbidden", 503)]
    [InlineData("server-error", 503)]
    public async Task ProviderFailuresReturnSafeManualFallback(string mode, int status)
    {
        HttpStatusCode providerStatus = mode switch
        {
            "none" => HttpStatusCode.NotFound,
            "unauthorized" => HttpStatusCode.Unauthorized,
            "forbidden" => HttpStatusCode.Forbidden,
            "server-error" => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.OK
        };
        string json = mode == "bad-json" ? "not json" : mode == "bad-shape" ? "{}" : "{\"features\":[]}";
        using var handler = new ProviderHandler(json, providerStatus, mode);
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        using var service = Create(handler, cache);
        ApiException error = await Assert.ThrowsAsync<ApiException>(() => service.ReverseAsync(14.64, 121.04, default));
        Assert.Equal(status, error.StatusCode);
        Assert.Contains("manually", error.Message);
        Assert.DoesNotContain("121.04", error.Message);
        Assert.DoesNotContain("test-key", error.Message);
        if (status == 404)
        {
            await Assert.ThrowsAsync<ApiException>(() => service.ReverseAsync(14.64, 121.04, default));
            Assert.Equal(1, handler.Calls);
        }
    }

    [Fact]
    public async Task MissingKeyDoesNotCallAnyProvider()
    {
        using var handler = new ProviderHandler(DetailedResult);
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        using var service = new ReverseGeocodingService(new FakeClients(handler, false), cache, TimeSpan.Zero, TimeSpan.FromDays(1));
        ApiException error = await Assert.ThrowsAsync<ApiException>(() => service.ReverseAsync(0, 0, default));
        Assert.Equal(503, error.StatusCode);
        Assert.Contains("not configured", error.Message);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task InvalidOrCanceledLookupsDoNotReachProvider_AndDoNotLockTheGate()
    {
        using var handler = new ProviderHandler(DetailedResult);
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        using var service = Create(handler, cache);
        Assert.Equal(400, (await Assert.ThrowsAsync<ApiException>(() => service.ReverseAsync(double.NaN, 0, default))).StatusCode);
        Assert.Equal(400, (await Assert.ThrowsAsync<ApiException>(() => service.ReverseAsync(0, 181, default))).StatusCode);
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ReverseAsync(0, 0, canceled.Token));
        Assert.Equal(0, handler.Calls);
        Assert.NotEmpty((await service.ReverseAsync(0, 0, default)).Address);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ReverseAsync(0, 0, canceled.Token));
    }

    private static ReverseGeocodingService Create(ProviderHandler handler, IMemoryCache cache)
    {
        return new ReverseGeocodingService(new FakeClients(handler), cache, TimeSpan.FromSeconds(1), TimeSpan.FromDays(1));
    }

    private sealed class FakeClients(ProviderHandler handler, bool configured = true) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.Equal("AzureMaps", name);
            var client = new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("https://provider.invalid/") };
            if (configured) client.DefaultRequestHeaders.Add("subscription-key", "test-key");
            return client;
        }
    }

    private sealed class ProviderHandler(string json, HttpStatusCode status = HttpStatusCode.OK, string mode = "") : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public Uri? Uri { get; private set; }
        public string? Key { get; private set; }
        public List<long> Starts { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++; Uri = request.RequestUri; Starts.Add(Stopwatch.GetTimestamp());
            Key = request.Headers.GetValues("subscription-key").Single();
            await Task.Delay(20, cancellationToken);
            if (mode == "offline") throw new HttpRequestException("Provider failed at private coordinates 14.64,121.04");
            if (mode == "timeout") throw new TaskCanceledException("Provider timeout at private coordinates 14.64,121.04");
            var response = new HttpResponseMessage(status) { Content = new StringContent(json) };
            if (status == HttpStatusCode.TooManyRequests) response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(60));
            return response;
        }
    }
}
