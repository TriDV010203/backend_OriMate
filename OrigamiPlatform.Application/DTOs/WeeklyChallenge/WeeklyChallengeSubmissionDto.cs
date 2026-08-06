namespace OrigamiPlatform.Application.DTOs.WeeklyChallenge;

public record WeeklyChallengeSubmissionDto(
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
