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
        private readonly string _seedPortraitsRoot;

        public LocalFileStorage(IConfiguration configuration, IWebHostEnvironment environment)
        {
            string root = configuration["Storage:Path"] ?? Path.Combine(environment.ContentRootPath, "data", "uploads");
            _root = Path.GetFullPath(root, environment.ContentRootPath);
            _seedPortraitsRoot = Path.Combine(environment.ContentRootPath, "Data", "SeedPortraits");
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

            string blobName = $"{Guid.NewGuid():N}.{extension}";
            string path = ResolveUpload(blobName);
            try
            {
                await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                encoded.SaveTo(output);
                await output.FlushAsync(cancellationToken);
                return blobName;
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

        public Task<StoredFile> OpenAsync(string blobName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidateBlobName(blobName);
            string path;
            if (SeedPortraitPattern().IsMatch(blobName))
            {
                // Bundled synthetic demo photos are read-only and need no external API at runtime.
                path = Path.Combine(_seedPortraitsRoot, blobName);
            }
            else
            {
                path = ResolveUpload(blobName);
            }
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
                    using (var codec = SKCodec.Create(path))
                    {
                        contentType = codec?.EncodedFormat switch
                        {
                            SKEncodedImageFormat.Jpeg => "image/jpeg",
                            SKEncodedImageFormat.Png => "image/png",
                            _ => "image/webp"
                        };
                    }
                    break;
            }
            var file = new StoredFile(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete), contentType);
            return Task.FromResult(file);
        }

        public async Task SetAvatarAsync(Guid userId, StoredFile photo, bool onlyIfMissing, CancellationToken cancellationToken)
        {
            string path = ResolveUpload($"avatars/{userId:N}/current");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await photo.Content.CopyToAsync(output, cancellationToken);
                }
                try
                {
                    File.Move(temporary, path, overwrite: !onlyIfMissing);
                }
                catch (IOException) when (onlyIfMissing && File.Exists(path))
                {
                    // A prior import or a concurrent upload already supplied this avatar.
                }
            }
            finally
            {
                File.Delete(temporary);
            }
        }

        public async Task SetGymCoverAsync(long gymId, StoredFile photo, CancellationToken cancellationToken)
        {
            string path = ResolveUpload($"gyms/{gymId}/cover");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await photo.Content.CopyToAsync(output, cancellationToken);
                }
                File.Move(temporary, path, overwrite: true);
            }
            finally
            {
                File.Delete(temporary);
            }
        }

        public Task DeleteAsync(string blobName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            File.Delete(ResolveUpload(blobName));
            return Task.CompletedTask;
        }

        private string ResolveUpload(string blobName)
        {
            if (!BlobNamePattern().IsMatch(blobName) && !AvatarNamePattern().IsMatch(blobName) && !GymCoverPattern().IsMatch(blobName))
            {
                throw new ApiException(404, "Photo not found.");
            }
            return Path.Combine(_root, blobName);
        }

        public static void ValidateBlobName(string blobName)
        {
            if (!BlobNamePattern().IsMatch(blobName) && !SeedPortraitPattern().IsMatch(blobName) && !AvatarNamePattern().IsMatch(blobName) && !GymCoverPattern().IsMatch(blobName))
            {
                throw new ApiException(404, "Photo not found.");
            }
        }

        [GeneratedRegex(@"\A[a-f0-9]{32}\.(jpg|png|webp)\z", RegexOptions.CultureInvariant)]
        private static partial Regex BlobNamePattern();

        [GeneratedRegex(@"\Aseed-[a-z]{2,16}\.webp\z", RegexOptions.CultureInvariant)]
        private static partial Regex SeedPortraitPattern();

        [GeneratedRegex(@"\Agyms/[1-9][0-9]*/cover\z", RegexOptions.CultureInvariant)]
        private static partial Regex GymCoverPattern();

        [GeneratedRegex(@"\Aavatars/[a-f0-9]{32}/current\z", RegexOptions.CultureInvariant)]
        private static partial Regex AvatarNamePattern();
    }
}
