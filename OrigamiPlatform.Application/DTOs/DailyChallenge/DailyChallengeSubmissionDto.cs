namespace OrigamiPlatform.Application.DTOs.DailyChallenge;

public record DailyChallengeSubmissionDto(
    Guid Id,
    Guid UserId,
    string? UserDisplayName,
    string? UserAvatarUrl,
    string PhotoUrl,
    string? Note,
    int LikeCount,
    bool IsLikedByCurrentUser,
    int? FinalRank,
    DateTime CreatedAt
);
