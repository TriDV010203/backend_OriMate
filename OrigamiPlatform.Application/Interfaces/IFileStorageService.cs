namespace OrigamiPlatform.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadImageAsync(Stream fileStream, string fileName, string folder, CancellationToken ct = default);
}
