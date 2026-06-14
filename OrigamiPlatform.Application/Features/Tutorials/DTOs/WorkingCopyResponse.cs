namespace OrigamiPlatform.Application.Features.Tutorials.DTOs;

public record WorkingCopyResponse(Guid WorkingCopyId, Guid OriginalId, string Status);
