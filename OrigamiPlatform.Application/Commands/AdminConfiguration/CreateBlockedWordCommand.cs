using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public record CreateBlockedWordCommand(Guid ActorId, CreateBlockedWordRequest Request);
