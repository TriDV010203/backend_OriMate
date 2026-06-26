namespace OrigamiPlatform.Application.Commands.TutorialProgress;

public record UncompleteTutorialStepCommand(Guid UserId, Guid TutorialId, Guid StepId);
