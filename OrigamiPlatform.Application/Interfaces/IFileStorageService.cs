namespace OrigamiPlatform.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadImageAsync(Stream fileStream, string fileName, string folder, CancellationToken ct = default);
    Task<string> UploadModel3DAsync(Stream fileStream, string fileName, string folder, CancellationToken ct = default);
}
