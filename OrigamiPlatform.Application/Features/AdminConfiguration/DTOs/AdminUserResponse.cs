namespace OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;

public record AdminUserResponse(
    Guid Id,
    string Email,
    string? DisplayName,
    string? AvatarUrl,
    string Status,
    List<string> Roles,
    DateTime CreatedAt);
