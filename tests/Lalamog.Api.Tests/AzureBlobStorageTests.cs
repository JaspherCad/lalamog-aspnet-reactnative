using System.Net;
using System.Diagnostics;
using System.Net.Http.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Lalamog.Api.Data;
using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Lalamog.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using Xunit;

namespace Lalamog.Api.Tests;

public class AzureBlobStorageTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _app;

    public AzureBlobStorageTests(ApiFixture app)
    {
        _app = app;
    }

    [Fact]
    public async Task PrivateBlobsServeUserDerivedAvatars_ImportLegacyFiles_AndPreserveReplacements()
    {
        string connection = Environment.GetEnvironmentVariable("LALAMOG_TEST_BLOB") ?? "UseDevelopmentStorage=true";
        string containerName = "lalamog-tests-" + Guid.NewGuid().ToString("N");
        var container = new BlobContainerClient(connection, containerName,
            new BlobClientOptions(BlobClientOptions.ServiceVersion.V2025_05_05));
        void ConfigureAzure(IWebHostBuilder builder)
        {
            builder.UseSetting("Storage:Provider", "AzureBlob");
            builder.UseSetting("Storage:Container", containerName);
            builder.UseSetting("ConnectionStrings:BlobStorage", connection);
        }

        try
        {
            using var app = _app.WithWebHostBuilder(ConfigureAzure);
            using var scope = app.Services.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
            var local = scope.ServiceProvider.GetRequiredService<LocalFileStorage>();
            Assert.IsType<AzureBlobStorage>(storage);
            using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });

            // Concurrent first reads import a bundled default once and preserve its public API URL.
            var seedReads = await Task.WhenAll(client.GetAsync("/api/media/seed-nico.webp"), client.GetAsync("/api/media/seed-nico.webp"));
            foreach (var seedRead in seedReads)
            {
                Assert.Equal(HttpStatusCode.OK, seedRead.StatusCode);
                Assert.Equal("image/webp", seedRead.Content.Headers.ContentType!.MediaType);
                Assert.Equal("nosniff", seedRead.Headers.GetValues("X-Content-Type-Options").Single());
                using var decoded = SKBitmap.Decode(await seedRead.Content.ReadAsByteArrayAsync());
                Assert.NotNull(decoded);
            }
            Assert.True((await container.GetBlobClient("seed-nico.webp").ExistsAsync()).Value);
            Assert.Equal(PublicAccessType.None, (await container.GetAccessPolicyAsync()).Value.BlobPublicAccess);
            using var anonymous = new HttpClient();
            Assert.False((await anonymous.GetAsync(container.GetBlobClient("seed-nico.webp").Uri)).IsSuccessStatusCode);

            await CsrfAsync(client);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("alex.seed@example.test", "Lalamog123!Test"))).StatusCode);
            await CsrfAsync(client);
            using var bitmap = new SKBitmap(2, 2);
            bitmap.Erase(SKColors.Blue);
            using var image = SKImage.FromBitmap(bitmap);
            byte[] png = Array.Empty<byte>();
            string lastUploadedUrl = "";
            byte[] lastUploadedBytes = Array.Empty<byte>();
            foreach (var format in new[] { SKEncodedImageFormat.Png, SKEncodedImageFormat.Jpeg, SKEncodedImageFormat.Webp })
            {
                using var encoded = image.Encode(format, 90);
                byte[] bytes = encoded.ToArray();
                if (format == SKEncodedImageFormat.Png)
                {
                    png = bytes;
                }
                using var body = new MultipartFormDataContent { { new ByteArrayContent(bytes), "file", "untrusted-filename.exe" } };
                var response = await client.PostAsync("/api/profiles/me/avatar", body);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var profile = (await response.Content.ReadFromJsonAsync<ProfileResponse>())!;
                Assert.StartsWith("/api/media/", profile.AvatarUrl);
                Assert.StartsWith($"/api/media/avatars/{profile.UserId:D}?v=", profile.AvatarUrl);
                string blobName = $"avatars/{profile.UserId:N}/current";
                Assert.True((await container.GetBlobClient(blobName).ExistsAsync()).Value);
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(profile.AvatarUrl)).StatusCode);
                lastUploadedUrl = profile.AvatarUrl!;
                lastUploadedBytes = await client.GetByteArrayAsync(profile.AvatarUrl);
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.ChangeTracker.Clear();
                Assert.Null(db.Model.FindEntityType(typeof(FighterProfile))!.FindProperty("AvatarBlobName"));
                Assert.Equal(0, await db.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID('Profiles') AND name IN ('AvatarKey','AvatarBlobName')").SingleAsync());
                Assert.True((await client.GetAsync(profile.AvatarUrl)).Headers.CacheControl!.NoStore);
                // The same owner path always serves the newest upload, regardless of prior browser URLs.
                Assert.Equal(lastUploadedBytes, await client.GetByteArrayAsync($"/api/media/avatars/{profile.UserId:D}"));
            }

            // A legacy object is copied byte-for-byte; its original stays available for rollback.
            var legacyFile = new FormFile(new MemoryStream(png), 0, png.Length, "file", "legacy.png");
            string legacyName = await local.SavePhotoAsync(legacyFile, CancellationToken.None);
            var legacy = await local.OpenAsync(legacyName, CancellationToken.None);
            byte[] original;
            await using (legacy.Content)
            {
                using var buffer = new MemoryStream();
                await legacy.Content.CopyToAsync(buffer);
                original = buffer.ToArray();
            }
            Assert.Equal(original, await client.GetByteArrayAsync("/api/media/" + legacyName));
            Assert.Equal(original, (await container.GetBlobClient(legacyName).DownloadContentAsync()).Value.Content.ToArray());
            var retained = await local.OpenAsync(legacyName, CancellationToken.None);
            await retained.Content.DisposeAsync();

            // A second service/client can read the persisted object without a local copy.
            using (var secondScope = app.Services.CreateScope())
            {
                var reopened = await secondScope.ServiceProvider.GetRequiredService<IFileStorage>().OpenAsync(legacyName, CancellationToken.None);
                await reopened.Content.DisposeAsync();
            }
            if (Environment.GetEnvironmentVariable("LALAMOG_RESTART_AZURITE") == "1")
            {
                Assert.Equal("UseDevelopmentStorage=true", connection);
                var start = new ProcessStartInfo("docker") { UseShellExecute = false, CreateNoWindow = true };
                start.ArgumentList.Add("restart");
                start.ArgumentList.Add("lalamog-azurite");
                using var restart = Process.Start(start)!;
                await restart.WaitForExitAsync();
                Assert.Equal(0, restart.ExitCode);
                // SDK retries cover the brief local emulator readiness gap.
                Assert.Equal(original, (await container.GetBlobClient(legacyName).DownloadContentAsync()).Value.Content.ToArray());
                Assert.Equal(original, await client.GetByteArrayAsync("/api/media/" + legacyName));
            }
            await storage.DeleteAsync(legacyName, CancellationToken.None);
            Assert.False((await container.GetBlobClient(legacyName).ExistsAsync()).Value);

            // Reseeding preserves custom storage bytes; an invalid replacement also leaves them intact.
            var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await DatabaseSeeder.SeedAsync(database);
            Assert.Equal(lastUploadedBytes, await client.GetByteArrayAsync(lastUploadedUrl));
            var alex = await database.Users.Include(u => u.Profile).SingleAsync(u => u.Email == "alex.seed@example.test");
            alex.Profile.FighterName = "Renamed-Azure-Fighter";
            alex.Profile.NormalizedFighterName = "RENAMED-AZURE-FIGHTER";
            await database.SaveChangesAsync();
            var renamed = (await client.GetFromJsonAsync<ProfileResponse>("/api/profiles/me"))!;
            Assert.Equal($"/api/media/avatars/{alex.Id:D}", renamed.AvatarUrl);
            Assert.Equal(lastUploadedBytes, await client.GetByteArrayAsync(renamed.AvatarUrl));
            var defaultPhoto = await storage.OpenAsync("seed-ana.webp", CancellationToken.None);
            await using (defaultPhoto.Content)
            {
                await storage.SetAvatarAsync(alex.Id, defaultPhoto, true, CancellationToken.None);
            }
            Assert.Equal(lastUploadedBytes, await client.GetByteArrayAsync(lastUploadedUrl));

            using var badBody = new MultipartFormDataContent { { new ByteArrayContent(new byte[20]), "file", "fake.png" } };
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/profiles/me/avatar", badBody)).StatusCode);
            Assert.Equal(lastUploadedBytes, await client.GetByteArrayAsync(lastUploadedUrl));
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/media/avatars/{Guid.NewGuid():D}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/media/seed-..%5cappsettings.json")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/media/seed-missing.webp")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/media/" + new string('f', 32) + ".png")).StatusCode);

            var unavailableContainer = new BlobContainerClient(new Uri("http://127.0.0.1:1/devstoreaccount1/unavailable"),
                new BlobClientOptions { Retry = { MaxRetries = 0 } });
            var unavailableStorage = new AzureBlobStorage(unavailableContainer, local);
            var unavailable = await Assert.ThrowsAsync<ApiException>(ReadUnavailableAsync);
            Assert.Equal(503, unavailable.StatusCode);
            async Task ReadUnavailableAsync()
            {
                await unavailableStorage.OpenAsync("seed-nico.webp", CancellationToken.None);
            }

            // Stop this application host and verify Azure-only content through a fresh host.
            client.Dispose();
            app.Dispose();
            using var restarted = _app.WithWebHostBuilder(ConfigureAzure);
            using var afterRestart = restarted.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            Assert.Equal(lastUploadedBytes, await afterRestart.GetByteArrayAsync(lastUploadedUrl));
        }
        finally
        {
            Assert.StartsWith("lalamog-tests-", containerName);
            await container.DeleteIfExistsAsync();
        }
    }

    private static async Task CsrfAsync(HttpClient client)
    {
        var csrf = (await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf"))!;
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.Token);
    }

}
