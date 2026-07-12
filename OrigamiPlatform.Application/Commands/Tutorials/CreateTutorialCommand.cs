using OrigamiPlatform.Application.Features.Tutorials.DTOs;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public record CreateTutorialCommand(Guid AuthorId, CreateTutorialRequest Request);
