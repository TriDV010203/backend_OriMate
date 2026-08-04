using System.Buffers.Binary;
using Moq;
using OrigamiPlatform.Application.Commands.Uploads;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Uploads;

public class UploadModel3DHandlerTests
{
    private readonly Mock<IFileStorageService> _fileStorage = new();

    private UploadModel3DHandler CreateHandler() => new(_fileStorage.Object);

    [Fact]
    public async Task HandleAsync_ValidGlb_UploadsToTutorialModelFolder()
    {
        var file = CreateGlb();
        _fileStorage
            .Setup(s => s.UploadModel3DAsync(file, "crane.glb", "tutorial-models", default))
            .ReturnsAsync("https://cdn.example.com/crane.glb");

        var result = await CreateHandler().HandleAsync(
            new UploadModel3DCommand(file, "crane.glb", "model/gltf-binary", file.Length));

        Assert.Equal("https://cdn.example.com/crane.glb", result);
        _fileStorage.Verify(s => s.UploadModel3DAsync(
            file,
            "crane.glb",
            "tutorial-models",
            default), Times.Once);
    }

    [Theory]
    [InlineData("crane.gltf")]
    [InlineData("crane.zip")]
    [InlineData("crane.glb.exe")]
    public async Task HandleAsync_InvalidExtension_RejectsBeforeUpload(string fileName)
    {
        var file = CreateGlb();

        await Assert.ThrowsAsync<DomainException>(() => CreateHandler().HandleAsync(
            new UploadModel3DCommand(file, fileName, "model/gltf-binary", file.Length)));

        VerifyNoUpload();
    }

    [Fact]
    public async Task HandleAsync_FileLargerThan25Mb_RejectsBeforeUpload()
    {
        var file = CreateGlb();

        await Assert.ThrowsAsync<DomainException>(() => CreateHandler().HandleAsync(
            new UploadModel3DCommand(
                file,
                "crane.glb",
                "model/gltf-binary",
                UploadModel3DHandler.MaxFileSizeBytes + 1)));

        VerifyNoUpload();
    }

    [Fact]
    public async Task HandleAsync_InvalidContentType_RejectsBeforeUpload()
    {
        var file = CreateGlb();

        await Assert.ThrowsAsync<DomainException>(() => CreateHandler().HandleAsync(
            new UploadModel3DCommand(file, "crane.glb", "image/png", file.Length)));

        VerifyNoUpload();
    }

    [Fact]
    public async Task HandleAsync_FileRenamedToGlbButInvalidHeader_RejectsBeforeUpload()
    {
        var invalidFile = new MemoryStream(new byte[12]);

        await Assert.ThrowsAsync<DomainException>(() => CreateHandler().HandleAsync(
            new UploadModel3DCommand(
                invalidFile,
                "fake.glb",
                "application/octet-stream",
                invalidFile.Length)));

        VerifyNoUpload();
    }

    [Fact]
    public async Task HandleAsync_UnsupportedGlbVersion_RejectsBeforeUpload()
    {
        var file = CreateGlb(version: 1);

        await Assert.ThrowsAsync<DomainException>(() => CreateHandler().HandleAsync(
            new UploadModel3DCommand(file, "legacy.glb", "model/gltf-binary", file.Length)));

        VerifyNoUpload();
    }

    [Fact]
    public async Task HandleAsync_DeclaredLengthDoesNotMatchFile_RejectsBeforeUpload()
    {
        var file = CreateGlb(declaredLength: 28);

        await Assert.ThrowsAsync<DomainException>(() => CreateHandler().HandleAsync(
            new UploadModel3DCommand(file, "broken.glb", "model/gltf-binary", file.Length)));

        VerifyNoUpload();
    }

    private void VerifyNoUpload()
        => _fileStorage.Verify(s => s.UploadModel3DAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);

    private static MemoryStream CreateGlb(uint version = 2, uint? declaredLength = null)
    {
        const int fileLength = 24;
        var bytes = new byte[fileLength];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0, 4), 0x46546C67);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4, 4), version);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), declaredLength ?? fileLength);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12, 4), 4);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16, 4), 0x4E4F534A);
        "{}  "u8.CopyTo(bytes.AsSpan(20, 4));
        return new MemoryStream(bytes);
    }
}
