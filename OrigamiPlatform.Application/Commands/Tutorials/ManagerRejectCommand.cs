using OrigamiPlatform.Application.Features.Tutorials.DTOs;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public record ManagerRejectCommand(Guid TutorialId, Guid ManagerId, ManagerRejectRequest Request);
