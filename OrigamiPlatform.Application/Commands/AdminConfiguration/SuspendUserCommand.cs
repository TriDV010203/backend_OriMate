using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public record SuspendUserCommand(Guid ActorId, Guid UserId, SuspendUserRequest Request);
