using OrigamiPlatform.Application.Features.Tutorials.DTOs;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public record ManagerRejectEditCommand(Guid WorkingCopyId, Guid ManagerId, ManagerRejectRequest Request);
