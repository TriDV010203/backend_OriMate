namespace OrigamiPlatform.Application.Commands.Auth;

public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword, string ConfirmPassword);
