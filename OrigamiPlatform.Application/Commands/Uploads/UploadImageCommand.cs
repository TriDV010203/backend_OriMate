namespace OrigamiPlatform.Application.Commands.Uploads;

public record UploadImageCommand(Stream FileStream, string FileName, string? ContentType, long FileSizeBytes, string Folder);
