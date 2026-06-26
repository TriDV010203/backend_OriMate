namespace OrigamiPlatform.Application.DTOs.Tutorials;

public record TutorialStepDto(
    Guid Id,
    int StepOrder,
    string Description,
    string? ImageUrl,
    bool IsLocked = false
);
