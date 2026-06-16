namespace OrigamiPlatform.Application.DTOs.Users;

public record CreatorProfileDto(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    int FollowerCount,
    int FollowingCount,
    int PostCount,
    int AchievementCount,
    bool IsFollowing,
    bool IsSuspended,
    List<string> Roles
);