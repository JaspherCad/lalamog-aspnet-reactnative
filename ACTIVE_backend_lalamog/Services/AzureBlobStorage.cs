using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;

namespace Lalamog.Api.Services;

public class AzureBlobStorage : IFileStorage
{
    private readonly BlobContainerClient _container;
    private readonly LocalFileStorage _localFiles;

    public AzureBlobStorage(BlobContainerClient container, LocalFileStorage localFiles)
    {
        _container = container;
        _localFiles = localFiles;
    }

    public async Task<string> SavePhotoAsync(IFormFile file, CancellationToken cancellationToken)
    {
        // The existing decoder validates and re-encodes the photo into a temporary local blob.
        string blobName = await _localFiles.SavePhotoAsync(file, cancellationToken);
        try
        {
            StoredFile photo = await _localFiles.OpenAsync(blobName, cancellationToken);
            await using (photo.Content)
            {
                await UploadAsync(blobName, photo, cancellationToken);
            }
            return blobName;
        }
        catch (RequestFailedException)
        {
            throw new ApiException(503, "Photo storage is unavailable. Please try again.");
        }
        finally
        {
            // Only this new staging file is removed; existing uploads remain untouched.
            await _localFiles.DeleteAsync(blobName, CancellationToken.None);
        }
    }

    public async Task<StoredFile> OpenAsync(string blobName, CancellationToken cancellationToken)
    {
        LocalFileStorage.ValidateBlobName(blobName);
        try
        {
            try
            {
                var download = await _container.GetBlobClient(blobName).DownloadStreamingAsync(cancellationToken: cancellationToken);
                return new StoredFile(download.Value.Content, download.Value.Details.ContentType);
            }
            catch (RequestFailedException exception) when (exception.Status == 404)
            {
                // Copy legacy uploads or bundled defaults on their first read, preserving their old API URL.
                StoredFile legacy = await _localFiles.OpenAsync(blobName, cancellationToken);
                try
                {
                    try
                    {
                        await UploadAsync(blobName, legacy, cancellationToken);
                    }
                    catch (RequestFailedException conflict) when (conflict.Status == 409 || conflict.Status == 412)
                    {
                        // Another request already imported the same immutable blob.
                    }
                    legacy.Content.Position = 0;
                    return legacy;
                }
                catch
                {
                    await legacy.Content.DisposeAsync();
                    throw;
                }
            }
        }
        catch (RequestFailedException)
        {
            throw new ApiException(503, "Photo storage is unavailable. Please try again.");
        }
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken)
    {
        LocalFileStorage.ValidateBlobName(blobName);
        try
        {
            await _container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException)
        {
            throw new ApiException(503, "Photo storage is unavailable. Please try again.");
        }
    }

    public async Task SetAvatarAsync(Guid userId, StoredFile photo, bool onlyIfMissing, CancellationToken cancellationToken)
    {
        try
        {
            await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
            await _container.GetBlobClient($"avatars/{userId:N}/current").UploadAsync(photo.Content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = photo.ContentType },
                Conditions = onlyIfMissing ? new BlobRequestConditions { IfNoneMatch = ETag.All } : null
            }, cancellationToken);
        }
        catch (RequestFailedException exception) when (onlyIfMissing && (exception.Status == 409 || exception.Status == 412))
        {
            // Import never overwrites an avatar already stored at its owner path.
        }
        catch (RequestFailedException)
        {
            throw new ApiException(503, "Photo storage is unavailable. Please try again.");
        }
    }

    private async Task UploadAsync(string blobName, StoredFile photo, CancellationToken cancellationToken)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        await _container.GetBlobClient(blobName).UploadAsync(photo.Content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = photo.ContentType },
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
        }, cancellationToken);
    }
}
