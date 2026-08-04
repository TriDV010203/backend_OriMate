namespace OrigamiPlatform.Application.Commands.WeeklyChallenge;

public record GradeWeeklyChallengeSubmissionCommand(
    Guid SubmissionId,
    Guid CollaboratorId,
    int RelevanceScore);
