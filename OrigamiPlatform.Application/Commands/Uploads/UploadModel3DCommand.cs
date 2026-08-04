namespace OrigamiPlatform.Application.Commands.Uploads;

public record UploadModel3DCommand(
    Stream FileStream,
    string FileName,
    string? ContentType,
    long FileSizeBytes);
