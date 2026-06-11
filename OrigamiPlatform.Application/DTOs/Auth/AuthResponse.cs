namespace OrigamiPlatform.Application.DTOs.Auth;

public record AuthResponse(
    Guid UserId,
    string Email,
    string? DisplayName,
    IEnumerable<string> Roles,
    string Token,
    DateTime ExpiresAt
);
