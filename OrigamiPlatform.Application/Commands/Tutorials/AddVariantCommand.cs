namespace OrigamiPlatform.Application.Commands.Tutorials;

public record AddVariantCommand(Guid RequesterId, Guid ParentTutorialId, Guid VariantTutorialId, int? DifficultyDelta);
