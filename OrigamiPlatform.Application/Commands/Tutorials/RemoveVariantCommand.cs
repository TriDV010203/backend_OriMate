namespace OrigamiPlatform.Application.Commands.Tutorials;

public record RemoveVariantCommand(Guid RequesterId, Guid ParentTutorialId, Guid VariantTutorialId);
