using OrigamiPlatform.Application.Features.Tutorials.DTOs;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public record ManagerRemoveCommand(Guid TutorialId, Guid ManagerId, ManagerRemoveRequest? Request);
