namespace Lalamog.Api.Interfaces;
public record StoredFile(Stream Content, string ContentType);
public interface IFileStorage
{
    Task<string> SavePhotoAsync(IFormFile file, CancellationToken ct);
    Task<StoredFile> OpenAsync(string key, CancellationToken ct);
    Task DeleteAsync(string key, CancellationToken ct);
}