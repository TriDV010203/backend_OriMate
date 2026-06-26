namespace OrigamiPlatform.Application.Commands.Users;

public record UpdateProfileCommand(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    string? Bio
);