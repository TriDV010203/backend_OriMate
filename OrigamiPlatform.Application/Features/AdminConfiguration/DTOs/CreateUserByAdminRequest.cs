namespace OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;

public record CreateUserByAdminRequest(string Email, string Password, string DisplayName, string Role);
