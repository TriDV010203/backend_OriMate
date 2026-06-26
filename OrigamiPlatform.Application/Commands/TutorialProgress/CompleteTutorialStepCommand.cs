namespace OrigamiPlatform.Application.Commands.TutorialProgress;

public record CompleteTutorialStepCommand(Guid UserId, Guid TutorialId, Guid StepId);
