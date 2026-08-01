using System.Buffers.Binary;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Uploads;

public class UploadModel3DHandler
{
    public const long MaxFileSizeBytes = 25 * 1024 * 1024;
    public const long MaxRequestSizeBytes = MaxFileSizeBytes + (1024 * 1024);

    private const int GlbHeaderSizeBytes = 12;
    private const int GlbChunkHeaderSizeBytes = 8;
    private const uint GlbMagic = 0x46546C67;
    private const uint SupportedGlbVersion = 2;
    private const uint JsonChunkType = 0x4E4F534A;
    private const string StorageFolder = "tutorial-models";

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "model/gltf-binary",
        "application/octet-stream"
    };

    private readonly IFileStorageService _fileStorage;

    public UploadModel3DHandler(IFileStorageService fileStorage) => _fileStorage = fileStorage;

    public async Task<string> HandleAsync(UploadModel3DCommand command, CancellationToken ct = default)
    {
        ValidateMetadata(command);

        MemoryStream? bufferedStream = null;
        var uploadStream = command.FileStream;

        try
        {
            if (!uploadStream.CanSeek)
            {
                bufferedStream = await BufferWithinLimitAsync(uploadStream, ct);
                uploadStream = bufferedStream;
            }

            var actualFileSize = uploadStream.Length;
            if (actualFileSize != command.FileSizeBytes)
                throw new DomainException("3D model size does not match the uploaded file.");

            await ValidateGlbHeaderAsync(uploadStream, actualFileSize, ct);
            uploadStream.Position = 0;

            return await _fileStorage.UploadModel3DAsync(
                uploadStream,
                command.FileName,
                StorageFolder,
                ct);
        }
        finally
        {
            if (bufferedStream is not null)
                await bufferedStream.DisposeAsync();
        }
    }

    private static void ValidateMetadata(UploadModel3DCommand command)
    {
        if (command.FileSizeBytes <= 0)
            throw new DomainException("File is empty.");

        if (command.FileSizeBytes > MaxFileSizeBytes)
            throw new DomainException("3D model must not exceed 25MB.");

        if (!string.Equals(Path.GetExtension(command.FileName), ".glb", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Only GLB 3D models are allowed.");

        if (command.ContentType is null || !AllowedContentTypes.Contains(command.ContentType))
            throw new DomainException("Invalid GLB content type.");
    }

    private static async Task ValidateGlbHeaderAsync(Stream stream, long actualFileSize, CancellationToken ct)
    {
        if (actualFileSize < GlbHeaderSizeBytes)
            throw new DomainException("The GLB file is incomplete.");

        stream.Position = 0;
        var header = new byte[GlbHeaderSizeBytes];
        await stream.ReadExactlyAsync(header, ct);

        var magic = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0, 4));
        var version = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4, 4));
        var declaredLength = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(8, 4));

        if (magic != GlbMagic)
            throw new DomainException("The uploaded file is not a valid GLB model.");

        if (version != SupportedGlbVersion)
            throw new DomainException("Only GLB version 2 is supported.");

        if (declaredLength != actualFileSize)
            throw new DomainException("The GLB file length is invalid.");

        if (actualFileSize < GlbHeaderSizeBytes + GlbChunkHeaderSizeBytes)
            throw new DomainException("The GLB file does not contain a JSON chunk.");

        var chunkHeader = new byte[GlbChunkHeaderSizeBytes];
        await stream.ReadExactlyAsync(chunkHeader, ct);

        var jsonChunkLength = BinaryPrimitives.ReadUInt32LittleEndian(chunkHeader.AsSpan(0, 4));
        var firstChunkType = BinaryPrimitives.ReadUInt32LittleEndian(chunkHeader.AsSpan(4, 4));
        var firstChunkEnd = GlbHeaderSizeBytes + GlbChunkHeaderSizeBytes + (long)jsonChunkLength;

        if (firstChunkType != JsonChunkType || jsonChunkLength == 0 || jsonChunkLength % 4 != 0)
            throw new DomainException("The GLB JSON chunk is invalid.");

        if (firstChunkEnd > actualFileSize)
            throw new DomainException("The GLB JSON chunk is incomplete.");
    }

    private static async Task<MemoryStream> BufferWithinLimitAsync(Stream source, CancellationToken ct)
    {
        var buffer = new byte[81920];
        var destination = new MemoryStream();

        try
        {
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, ct)) > 0)
            {
                if (destination.Length + bytesRead > MaxFileSizeBytes)
                    throw new DomainException("3D model must not exceed 25MB.");

                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            }

            destination.Position = 0;
            return destination;
        }
        catch
        {
            await destination.DisposeAsync();
            throw;
        }
    }
}
