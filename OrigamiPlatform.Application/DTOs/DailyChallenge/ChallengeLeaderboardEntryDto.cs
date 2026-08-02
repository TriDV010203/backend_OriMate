namespace OrigamiPlatform.Application.DTOs.DailyChallenge;

public record ChallengeLeaderboardEntryDto(
    int Rank,
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    int CurrentStreak,
    int LongestStreak
);
