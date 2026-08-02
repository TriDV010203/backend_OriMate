using OrigamiPlatform.Application.Features.Tutorials.DTOs;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public record UpdateTutorialCommand(Guid TutorialId, Guid AuthorId, UpdateTutorialRequest Request);
