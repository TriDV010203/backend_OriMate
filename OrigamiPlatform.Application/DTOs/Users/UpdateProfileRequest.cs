namespace OrigamiPlatform.Application.DTOs.Users;

public record UpdateProfileRequest(
    string DisplayName,
    string? AvatarUrl,
    string? Bio
);