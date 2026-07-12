namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public record RemoveBlockedWordCommand(Guid ActorId, int BlockedWordId);
