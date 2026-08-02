using OrigamiPlatform.Application.Features.Tutorials.DTOs;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public record UpdateWorkingCopyCommand(Guid WorkingCopyId, Guid AuthorId, UpdateTutorialRequest Request);
