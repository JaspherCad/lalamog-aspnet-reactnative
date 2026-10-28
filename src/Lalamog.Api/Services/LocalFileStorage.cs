using System.Text.RegularExpressions;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using SkiaSharp;
namespace Lalamog.Api.Services;
public partial class LocalFileStorage : IFileStorage
{
    private readonly string root;
    public LocalFileStorage(IConfiguration config, IWebHostEnvironment environment)
    {
        root = Path.GetFullPath(config["Storage:Path"] ?? Path.Combine(environment.ContentRootPath, "data", "uploads"));
        Directory.CreateDirectory(root);
    }
    public async Task<string> SavePhotoAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length is < 12 or > 5 * 1024 * 1024)
            throw new ApiException(400, "Choose a JPEG, PNG, or WebP photo up to 5 MB.");
        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, ct);
        if (buffer.Length > 5 * 1024 * 1024) throw new ApiException(400, "The photo exceeds 5 MB.");
        using var data = SKData.CreateCopy(buffer.ToArray());
        using var codec = SKCodec.Create(data);
        if (codec is null) throw new ApiException(400, "The photo is invalid or corrupt.");
        var format = codec.EncodedFormat;
        var extension = format switch
        {
            SKEncodedImageFormat.Jpeg => "jpg", SKEncodedImageFormat.Png => "png",
            SKEncodedImageFormat.Webp => "webp", _ => throw new ApiException(400, "Choose a JPEG, PNG, or WebP photo.")
        };
        var info = codec.Info;
        if (info.Width <= 0 || info.Height <= 0 || info.Width > 8192 || info.Height > 8192
            || (long)info.Width * info.Height > 20_000_000)
            throw new ApiException(400, "Choose a photo with at most 20 megapixels and 8192 pixels per side.");
        using var bitmap = new SKBitmap(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
            throw new ApiException(400, "The photo is invalid or corrupt.");
        // Re-encode decoded pixels so uploaded metadata and trailing content are not served.
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(format, 90);
        if (encoded is null) throw new ApiException(400, "This photo could not be encoded.");
        var key = $"{Guid.NewGuid():N}.{extension}";
        var path = Resolve(key);
        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            encoded.SaveTo(output);
            await output.FlushAsync(ct);
            return key;
        }
        catch { if (File.Exists(path)) File.Delete(path); throw; }
    }
    public Task<StoredFile> OpenAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var path = Resolve(key);
        if (!File.Exists(path)) throw new ApiException(404, "Photo not found.");
        var type = Path.GetExtension(path) switch { ".jpg" => "image/jpeg", ".png" => "image/png", _ => "image/webp" };
        return Task.FromResult(new StoredFile(File.OpenRead(path), type));
    }
    public Task DeleteAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); File.Delete(Resolve(key)); return Task.CompletedTask;
    }
    private string Resolve(string key)
    {
        if (!KeyPattern().IsMatch(key)) throw new ApiException(404, "Photo not found.");
        return Path.Combine(root, key);
    }
    [GeneratedRegex(@"\A[a-f0-9]{32}\.(jpg|png|webp)\z", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();
}