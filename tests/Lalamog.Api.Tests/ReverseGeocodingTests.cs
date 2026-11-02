using System.Diagnostics;
using System.Net;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Lalamog.Api.Tests;

public class ReverseGeocodingTests
{
    [Fact]
    public async Task ConcurrentIdenticalPinsShareOneLookup_AndKeepLegacyAddressOrder()
    {
        using var handler = new ProviderHandler("{\"address\":{\"road\":\"East Avenue\",\"suburb\":\"Pinyahan\",\"city\":\"Quezon City\",\"state\":\"Metro Manila\",\"country\":\"Philippines\"}}");
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        using var service = Create(handler, cache);
        var calls = await Task.WhenAll(service.ReverseAsync(14.640001, 121.040001, default), service.ReverseAsync(14.640002, 121.040002, default));
        Assert.Equal(1, handler.Calls);
        Assert.All(calls, result => Assert.Equal("East Avenue, Pinyahan, Quezon City, Metro Manila, Philippines", result.Address));
        Assert.Contains("lat=14.64000&lon=121.04000", handler.Uri!.Query);
        Assert.Contains("addressdetails=1", handler.Uri.Query);
        Assert.Equal("Pinyahan", calls[0].Suburb);
    }

    [Theory]
    [InlineData("{\"address\":{\"neighbourhood\":\"Barangay Example\",\"town\":\"Example Town\",\"country\":\"Philippines\"}}", "Barangay Example, Example Town, Philippines")]
    [InlineData("{\"address\":{\"quarter\":\"Example Quarter\",\"village\":\"Example Village\"}}", "Example Quarter, Example Village")]
    [InlineData("{\"display_name\":\"Available map label\"}", "Available map label")]
    public async Task MissingComponentsUseAvailableLabels(string json, string expected)
    {
        using var handler = new ProviderHandler(json);
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        using var service = Create(handler, cache);
        Assert.Equal(expected, (await service.ReverseAsync(0, 0, default)).Address);
    }

    [Fact]
    public async Task DistinctUsersPinsAreSerializedAtOneRequestPerSecond()
    {
        using var handler = new ProviderHandler("{\"display_name\":\"Public test place\"}");
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        using var service = Create(handler, cache);
        await Task.WhenAll(service.ReverseAsync(14.64, 121.04, default), service.ReverseAsync(14.65, 121.05, default));
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
    [InlineData("bad-json", 503)]
    [InlineData("offline", 503)]
    [InlineData("timeout", 503)]
    public async Task ProviderFailuresReturnSafeManualFallback(string mode, int status)
    {
        using var handler = new ProviderHandler(mode == "bad-json" ? "not json" : "{}", mode == "none" ? HttpStatusCode.NotFound : HttpStatusCode.OK, mode);
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        using var service = Create(handler, cache);
        ApiException error = await Assert.ThrowsAsync<ApiException>(() => service.ReverseAsync(14.64, 121.04, default));
        Assert.Equal(status, error.StatusCode);
        Assert.Contains("manually", error.Message);
        Assert.DoesNotContain("121.04", error.Message);
        if (mode == "none")
        {
            await Assert.ThrowsAsync<ApiException>(() => service.ReverseAsync(14.64, 121.04, default));
            Assert.Equal(1, handler.Calls);
        }
    }

    [Fact]
    public async Task InvalidOrCanceledLookupsDoNotReachProvider_AndDoNotLockTheGate()
    {
        using var handler = new ProviderHandler("{\"display_name\":\"Public test place\"}");
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        using var service = Create(handler, cache);
        Assert.Equal(400, (await Assert.ThrowsAsync<ApiException>(() => service.ReverseAsync(double.NaN, 0, default))).StatusCode);
        Assert.Equal(400, (await Assert.ThrowsAsync<ApiException>(() => service.ReverseAsync(0, 181, default))).StatusCode);
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ReverseAsync(0, 0, canceled.Token));
        Assert.Equal(0, handler.Calls);
        Assert.Equal("Public test place", (await service.ReverseAsync(0, 0, default)).Address);
    }

    private static ReverseGeocodingService Create(ProviderHandler handler, IMemoryCache cache)
    {
        return new ReverseGeocodingService(new FakeClients(handler), cache, TimeSpan.FromSeconds(1), TimeSpan.FromDays(1));
    }

    private sealed class FakeClients(ProviderHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://provider.invalid/") };
    }

    private sealed class ProviderHandler(string json, HttpStatusCode status = HttpStatusCode.OK, string mode = "") : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public Uri? Uri { get; private set; }
        public List<long> Starts { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++; Uri = request.RequestUri; Starts.Add(Stopwatch.GetTimestamp());
            await Task.Delay(20, cancellationToken);
            if (mode == "offline") throw new HttpRequestException("Provider failed at private coordinates 14.64,121.04");
            if (mode == "timeout") throw new TaskCanceledException("Provider timeout at private coordinates 14.64,121.04");
            var response = new HttpResponseMessage(status) { Content = new StringContent(json) };
            if (status == HttpStatusCode.TooManyRequests) response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(60));
            return response;
        }
    }
}
