using System.Text.RegularExpressions;
using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using SkiaSharp;

namespace Lalamog.Api.Services
{
    public partial class LocalFileStorage : IFileStorage
    {
        private readonly string _root;

        public LocalFileStorage(IConfiguration configuration, IWebHostEnvironment environment)
        {
            string root = configuration["Storage:Path"] ?? Path.Combine(environment.ContentRootPath, "data", "uploads");
            _root = Path.GetFullPath(root);
            Directory.CreateDirectory(_root);
        }

        public async Task<string> SavePhotoAsync(IFormFile file, CancellationToken cancellationToken)
        {
            if (file.Length < 12 || file.Length > 5 * 1024 * 1024)
            {
                throw new ApiException(400, "Choose a JPEG, PNG, or WebP photo up to 5 MB.");
            }

            await using var input = file.OpenReadStream();
            using var buffer = new MemoryStream();
            await input.CopyToAsync(buffer, cancellationToken);
            if (buffer.Length > 5 * 1024 * 1024)
            {
                throw new ApiException(400, "The photo exceeds 5 MB.");
            }

            using var data = SKData.CreateCopy(buffer.ToArray());
            using var codec = SKCodec.Create(data);
            if (codec == null)
            {
                throw new ApiException(400, "The photo is invalid or corrupt.");
            }

            SKEncodedImageFormat format = codec.EncodedFormat;
            string extension;
            switch (format)
            {
                case SKEncodedImageFormat.Jpeg:
                    extension = "jpg";
                    break;
                case SKEncodedImageFormat.Png:
                    extension = "png";
                    break;
                case SKEncodedImageFormat.Webp:
                    extension = "webp";
                    break;
                default:
                    throw new ApiException(400, "Choose a JPEG, PNG, or WebP photo.");
            }

            SKImageInfo info = codec.Info;
            if (info.Width <= 0 || info.Height <= 0 || info.Width > 8192 || info.Height > 8192
                || (long)info.Width * info.Height > 20_000_000)
            {
                throw new ApiException(400, "Choose a photo with at most 20 megapixels and 8192 pixels per side.");
            }

            using var bitmap = new SKBitmap(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
            {
                throw new ApiException(400, "The photo is invalid or corrupt.");
            }

            // Serve re-encoded pixels, never uploaded metadata or trailing content.
            using var image = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(format, 90);
            if (encoded == null)
            {
                throw new ApiException(400, "This photo could not be encoded.");
            }

            string key = $"{Guid.NewGuid():N}.{extension}";
            string path = Resolve(key);
            try
            {
                await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                encoded.SaveTo(output);
                await output.FlushAsync(cancellationToken);
                return key;
            }
            catch
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                throw;
            }
        }

        public Task<StoredFile> OpenAsync(string key, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string path = Resolve(key);
            if (!File.Exists(path))
            {
                throw new ApiException(404, "Photo not found.");
            }

            string contentType;
            switch (Path.GetExtension(path))
            {
                case ".jpg":
                    contentType = "image/jpeg";
                    break;
                case ".png":
                    contentType = "image/png";
                    break;
                default:
                    contentType = "image/webp";
                    break;
            }
            var file = new StoredFile(File.OpenRead(path), contentType);
            return Task.FromResult(file);
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            File.Delete(Resolve(key));
            return Task.CompletedTask;
        }

        private string Resolve(string key)
        {
            if (!KeyPattern().IsMatch(key))
            {
                throw new ApiException(404, "Photo not found.");
            }
            return Path.Combine(_root, key);
        }

        [GeneratedRegex(@"\A[a-f0-9]{32}\.(jpg|png|webp)\z", RegexOptions.CultureInvariant)]
        private static partial Regex KeyPattern();
    }
}
