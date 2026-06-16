namespace OrigamiPlatform.Application.Commands.FamilyProjects;

public record CompleteStepCommand(Guid ProjectId, Guid StepId, Guid UserId);
