using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public record RemoveRoleCommand(Guid ActorId, Guid UserId, RemoveRoleRequest Request);
