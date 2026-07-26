namespace OrigamiPlatform.Application.DTOs.DailyChallenge;

public record ChallengeResultDto(
    DateOnly ChallengeDate,
    string Status,
    Guid TutorialId,
    string TutorialTitle,
    int TotalParticipants,
    IReadOnlyList<DailyChallengeSubmissionDto> TopSubmissions
);
