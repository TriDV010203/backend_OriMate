namespace OrigamiPlatform.Application.Commands.Auth;

public record ResetPasswordCommand(string Token, string NewPassword);
