namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public record DeleteCategoryCommand(Guid ActorId, int CategoryId);
