using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Infrastructure.Services;

public class CloudinaryFileStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryFileStorageService(IConfiguration config)
    {
        var account = new Account(
            config["Cloudinary:CloudName"],
            config["Cloudinary:ApiKey"],
            config["Cloudinary:ApiSecret"]);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadImageAsync(Stream fileStream, string fileName, string folder, CancellationToken ct = default)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = $"orimate/{folder}",
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams, ct);

        if (result.Error is not null)
            throw new DomainException($"Image upload failed: {result.Error.Message}");

        return result.SecureUrl.ToString();
    }

    public async Task<string> UploadModel3DAsync(
        Stream fileStream,
        string fileName,
        string folder,
        CancellationToken ct = default)
    {
        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = $"orimate/{folder}",
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false,
            AllowedFormats = ["glb"]
        };

        var result = await _cloudinary.UploadAsync(uploadParams, "raw", ct);

        if (result.Error is not null)
            throw new DomainException($"3D model upload failed: {result.Error.Message}");

        return result.SecureUrl.ToString();
    }
}
