namespace OrigamiPlatform.Application.DTOs.Users;

public record FollowerUserDto(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    int FollowerCount,
    int TutorialCount,
    bool IsFollowing,
    List<string> Roles
);
