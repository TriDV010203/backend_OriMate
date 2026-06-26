namespace OrigamiPlatform.Application.DTOs.FamilyProjects;

public record StepProgressDto(
    Guid StepId,
    Guid CompletedBy,
    DateTime CompletedAt);
