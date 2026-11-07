using Lalamog.Api.Data;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Lalamog.Api.Repositories;
using Lalamog.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using Xunit;

namespace Lalamog.Api.Tests;

public class AvatarMigrationTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _app;

    public AvatarMigrationTests(ApiFixture app)
    {
        _app = app;
    }

    [Fact]
    public async Task TransferPreservesBytesAndOriginals_BlocksMissingMedia_ThenRemovesSqlReferences()
    {
        string database = "LalamogTests_AvatarUpgrade_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder(_app.Connection) { InitialCatalog = database };
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString).Options);
        using var scope = _app.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var profiles = new ProfileService(new ProfileRepository(db), storage, scope.ServiceProvider.GetRequiredService<IAreaService>());
        IMigrator migrator = db.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync("20261104085551_AvatarBlobNames");
            var user = new AppUser { Email = "migration@example.test", NormalizedEmail = "MIGRATION@EXAMPLE.TEST" };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            db.Profiles.Add(new FighterProfile { UserId = user.Id, Nickname = "Migration" });
            await db.SaveChangesAsync();
            string missing = new string('f', 32) + ".png";
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Profiles SET AvatarBlobName = {missing} WHERE UserId = {user.Id}");

            await Assert.ThrowsAsync<SqlException>(ApplyLatestAsync);
            var failure = await Assert.ThrowsAsync<ApiException>(TransferAsync);
            Assert.Equal(404, failure.StatusCode);
            Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM Profiles WHERE AvatarBlobName IS NOT NULL").SingleAsync());

            using var bitmap = new SKBitmap(2, 2);
            bitmap.Erase(SKColors.Green);
            using var image = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 90);
            byte[] bytes = encoded.ToArray();
            string originalName = await storage.SavePhotoAsync(new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "legacy.png"), CancellationToken.None);
            byte[] original = await ReadAsync(originalName);
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Profiles SET AvatarBlobName = {originalName} WHERE UserId = {user.Id}");
            await TransferAsync();
            await TransferAsync();
            Assert.Equal(original, await ReadAsync($"avatars/{user.Id:N}/current"));
            Assert.Equal(original, await ReadAsync(originalName));
            await ApplyLatestAsync();
            Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID('Profiles') AND name IN ('AvatarKey','AvatarBlobName')").SingleAsync());
            await TransferAsync(); // The transfer is also harmless after the column is gone.
            Assert.Equal(original, await ReadAsync($"avatars/{user.Id:N}/current"));
            Assert.False(db.Database.HasPendingModelChanges());
        }
        finally
        {
            if (!database.StartsWith("LalamogTests_AvatarUpgrade_", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Unexpected avatar migration database.");
            }
            await db.Database.EnsureDeletedAsync();
        }

        Task ApplyLatestAsync()
        {
            return migrator.MigrateAsync();
        }

        Task TransferAsync()
        {
            return profiles.MigrateLegacyAvatarsAsync(CancellationToken.None);
        }

        async Task<byte[]> ReadAsync(string name)
        {
            var photo = await storage.OpenAsync(name, CancellationToken.None);
            await using (photo.Content)
            {
                using var buffer = new MemoryStream();
                await photo.Content.CopyToAsync(buffer);
                return buffer.ToArray();
            }
        }
    }
}
