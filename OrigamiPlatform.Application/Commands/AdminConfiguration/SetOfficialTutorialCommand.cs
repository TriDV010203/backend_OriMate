namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public record SetOfficialTutorialCommand(Guid ActorId, Guid TutorialId, bool IsOfficial);
