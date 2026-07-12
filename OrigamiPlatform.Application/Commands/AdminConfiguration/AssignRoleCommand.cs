using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public record AssignRoleCommand(Guid ActorId, Guid UserId, AssignRoleRequest Request);
