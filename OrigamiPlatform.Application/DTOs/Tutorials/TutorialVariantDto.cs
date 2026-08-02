namespace OrigamiPlatform.Application.DTOs.Tutorials;

public record TutorialVariantDto(
    Guid VariantTutorialId,
    string Title,
    string Difficulty,
    int? DifficultyDelta,
    string Slug
);
