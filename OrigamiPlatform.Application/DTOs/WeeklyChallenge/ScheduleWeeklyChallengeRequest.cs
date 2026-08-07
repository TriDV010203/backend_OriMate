namespace OrigamiPlatform.Application.DTOs.WeeklyChallenge;

public record ScheduleWeeklyChallengeRequest(DateOnly ChallengeDate, Guid TutorialId);
