using System.Net;
using System.Net.Http.Json;
using Lalamog.Api.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lalamog.Api.Tests;

public class GeocodingApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task ReverseLookupRequiresCookieCsrfAndValidCoordinates_AndUsesConfiguredClient()
    {
        using var handler = new FakeProvider();
        using var app = fixture.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddHttpClient("Nominatim").ConfigurePrimaryHttpMessageHandler(() => handler)));
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var request = new ReverseGeocodeRequest { Latitude = 14.64, Longitude = 121.04 };
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/maps/reverse", request)).StatusCode);
        await Csrf(client);
        var registration = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(Guid.NewGuid().ToString("N") + "@example.test", "Practice123!Example", "Geocoder tester"));
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/maps/reverse", request)).StatusCode);
        await Csrf(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/maps/reverse", new { latitude = 14.64 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/maps/reverse", new { latitude = 91, longitude = 121.04 })).StatusCode);
        Assert.Equal(0, handler.Calls);
        var response = await client.PostAsJsonAsync("/api/maps/reverse", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        var result = await response.Content.ReadFromJsonAsync<ReverseGeocodeResponse>();
        Assert.Equal("Test Road, Pinyahan, Quezon City, Philippines", result!.Address);
        Assert.Equal("Pinyahan", result.Suburb);
        Assert.Contains("Lalamog/1.0", handler.UserAgent);
        Assert.Equal("nominatim.openstreetmap.org", handler.Host);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/maps/reverse", request)).StatusCode);
        Assert.Equal(1, handler.Calls);
    }

    private static async Task Csrf(HttpClient client)
    {
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.Token);
    }

    private sealed class FakeProvider : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string UserAgent { get; private set; } = "";
        public string Host { get; private set; } = "";
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++; UserAgent = request.Headers.UserAgent.ToString(); Host = request.RequestUri!.Host;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"address\":{\"road\":\"Test Road\",\"suburb\":\"Pinyahan\",\"city\":\"Quezon City\",\"country\":\"Philippines\"}}") });
        }
    }
}
