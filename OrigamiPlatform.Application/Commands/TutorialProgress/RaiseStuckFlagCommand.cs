namespace OrigamiPlatform.Application.Commands.TutorialProgress;

public record RaiseStuckFlagCommand(Guid UserId, Guid TutorialId, Guid StepId);
