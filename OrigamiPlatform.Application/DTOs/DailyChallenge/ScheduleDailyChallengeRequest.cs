namespace OrigamiPlatform.Application.DTOs.DailyChallenge;

public record ScheduleDailyChallengeRequest(DateOnly ChallengeDate, Guid TutorialId);
