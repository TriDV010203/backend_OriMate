namespace OrigamiPlatform.Application.DTOs.Auth;

public record AuthResponse(
    Guid UserId,
    string Email,
    IEnumerable<string> Roles,
    string Token,
    DateTime ExpiresAt
);
