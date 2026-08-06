namespace OrigamiPlatform.Application.Commands.LearningPathModes;

public record RejectModeUnlockSubmissionCommand(Guid SubmissionId, Guid ActorId, string Reason);
