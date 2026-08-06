namespace OrigamiPlatform.Application.DTOs.Tutorials;

public record AddVariantRequest(Guid VariantTutorialId, int? DifficultyDelta);
