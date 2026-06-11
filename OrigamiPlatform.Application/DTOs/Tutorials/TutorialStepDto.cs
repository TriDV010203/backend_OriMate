namespace OrigamiPlatform.Application.DTOs.Tutorials;

public record TutorialStepDto(
    Guid Id,
    int StepOrder,
    string Title,
    string Content,
    string? MediaUrl,
    bool IsLocked = false
);
