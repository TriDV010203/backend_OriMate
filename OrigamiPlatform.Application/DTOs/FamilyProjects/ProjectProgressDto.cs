namespace OrigamiPlatform.Application.DTOs.FamilyProjects;

public record ProjectProgressDto(
    Guid ProjectId,
    int TotalSteps,
    int CompletedSteps,
    bool AllStepsCompleted,
    IReadOnlyList<StepStatusDto> Steps,
    IReadOnlyList<MemberProgressDto> Members);

public record StepStatusDto(
    Guid StepId,
    int StepOrder,
    bool Completed,
    IReadOnlyList<Guid> CompletedBy);

public record MemberProgressDto(
    Guid UserId,
    string? Email,
    int StepsCompleted);
