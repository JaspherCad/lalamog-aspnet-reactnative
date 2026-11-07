using System.Net;
using System.Net.Http.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using Xunit;
namespace Lalamog.Api.Tests;
public class GymCoverTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _app;
    public GymCoverTests(ApiFixture app) { _app=app; }
    private static async Task CsrfAsync(HttpClient client)
    { var csrf=(await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf"))!;client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");client.DefaultRequestHeaders.Add("X-CSRF-TOKEN",csrf.Token); }
    [Fact]
    public async Task CoversUsePrivateAzuriteStorage_WithOwnerIsolation_Publication_ReplacementAndOutageSafety()
    {
        string connection=Environment.GetEnvironmentVariable("LALAMOG_TEST_BLOB")??"UseDevelopmentStorage=true";
        string name="lalamog-gym-tests-"+Guid.NewGuid().ToString("N");
        var container=new BlobContainerClient(connection,name,new BlobClientOptions(BlobClientOptions.ServiceVersion.V2025_05_05));
        void ConfigureAzure(IWebHostBuilder host)
        { host.UseSetting("Storage:Provider","AzureBlob");host.UseSetting("Storage:Container",name);host.UseSetting("ConnectionStrings:BlobStorage",connection); }
        try
        {
            using var app=_app.WithWebHostBuilder(ConfigureAzure);
            using var owner=app.CreateClient(new(){BaseAddress=new Uri("https://localhost"),HandleCookies=true});
            using var observer=app.CreateClient(new(){BaseAddress=new Uri("https://localhost"),HandleCookies=true});
            using var anonymous=app.CreateClient(new(){BaseAddress=new Uri("https://localhost")});
            foreach(var client in new[]{owner,observer})
            {
                await CsrfAsync(client);Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/auth/register",new RegisterRequest(Guid.NewGuid().ToString("N")+"@example.test","Practice123!Example","Cover owner"){AccountType="gymOwner"})).StatusCode);await CsrfAsync(client);
            }
            var created=await owner.PostAsJsonAsync("/api/gyms",new GymRequest {Slug="cover-"+Guid.NewGuid().ToString("N"),Name="Cover test gym",Address="Fictional public gym",AreaCode="0405802000",Latitude=14.6,Longitude=121.2});
            Assert.Equal(HttpStatusCode.OK,created.StatusCode);var gym=(await created.Content.ReadFromJsonAsync<GymResponse>())!;
            byte[] finalBytes=[];
            foreach(var format in new[]{SKEncodedImageFormat.Png,SKEncodedImageFormat.Jpeg,SKEncodedImageFormat.Webp})
            {
                using var bitmap=new SKBitmap(24,16);bitmap.Erase(format==SKEncodedImageFormat.Png?SKColors.Green:SKColors.Blue);
                using var image=SKImage.FromBitmap(bitmap);using var encoded=image.Encode(format,90);
                using var unauthorized=new MultipartFormDataContent{{new ByteArrayContent(encoded.ToArray()),"file","cover.png"}};
                Assert.Equal(HttpStatusCode.NotFound,(await observer.PostAsync($"/api/gyms/{gym.Id}/cover",unauthorized)).StatusCode);
                using var upload=new MultipartFormDataContent{{new ByteArrayContent(encoded.ToArray()),"file","cover.png"}};
                Assert.Equal(HttpStatusCode.NoContent,(await owner.PostAsync($"/api/gyms/{gym.Id}/cover",upload)).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound,(await anonymous.GetAsync(gym.CoverUrl)).StatusCode);
                var own=await owner.GetAsync(gym.CoverUrl);Assert.Equal(HttpStatusCode.OK,own.StatusCode);finalBytes=await own.Content.ReadAsByteArrayAsync();Assert.NotNull(SKBitmap.Decode(finalBytes));
            }
            Assert.Equal(PublicAccessType.None,(await container.GetAccessPolicyAsync()).Value.BlobPublicAccess);
            Assert.True((await container.GetBlobClient($"gyms/{gym.Id}/cover").ExistsAsync()).Value);
            Assert.Equal(HttpStatusCode.NotFound,(await owner.GetAsync($"/api/media/gyms/{gym.Id}/cover")).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await owner.PostAsJsonAsync($"/api/gyms/{gym.Id}/publish",new{})).StatusCode);
            Assert.Equal(finalBytes,await anonymous.GetByteArrayAsync(gym.CoverUrl));
            using var invalid=new MultipartFormDataContent{{new ByteArrayContent(new byte[20]),"file","bad.png"}};
            Assert.Equal(HttpStatusCode.BadRequest,(await owner.PostAsync($"/api/gyms/{gym.Id}/cover",invalid)).StatusCode);
            Assert.Equal(finalBytes,await anonymous.GetByteArrayAsync(gym.CoverUrl));
            using var restarted=_app.WithWebHostBuilder(ConfigureAzure);using var after=restarted.CreateClient(new(){BaseAddress=new Uri("https://localhost")});
            Assert.Equal(finalBytes,await after.GetByteArrayAsync(gym.CoverUrl));
            using var scope=app.Services.CreateScope();var local=scope.ServiceProvider.GetRequiredService<LocalFileStorage>();
            var broken=new AzureBlobStorage(new BlobContainerClient(new Uri("http://127.0.0.1:1/devstoreaccount1/unavailable"),new BlobClientOptions{Retry={MaxRetries=0}}),local);
            var failure=await Assert.ThrowsAsync<ApiException>(async()=>{await broken.SetGymCoverAsync(gym.Id,new StoredFile(new MemoryStream(finalBytes),"image/png"),CancellationToken.None);});Assert.Equal(503,failure.StatusCode);
        }
        finally { await container.DeleteIfExistsAsync(); }
    }
}
