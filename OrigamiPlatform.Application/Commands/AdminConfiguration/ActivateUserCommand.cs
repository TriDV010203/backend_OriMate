namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public record ActivateUserCommand(Guid ActorId, Guid UserId);
