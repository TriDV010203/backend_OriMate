namespace OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;

public record CategoryResponse(int Id, string Name, bool IsActive, DateTime CreatedAt);
