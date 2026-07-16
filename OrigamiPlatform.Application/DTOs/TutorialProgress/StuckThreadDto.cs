namespace OrigamiPlatform.Application.DTOs.TutorialProgress;

public record StuckThreadDto(
    Guid Id,
    Guid TutorialId,
    Guid StepId,
    Guid UserId,
    DateTime CreatedAt);
