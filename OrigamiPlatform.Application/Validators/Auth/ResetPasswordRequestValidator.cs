using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Validators.Auth;

public static class ResetPasswordRequestValidator
{
    public static void Validate(string token, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new DomainException("Token is required.");

        if (string.IsNullOrWhiteSpace(newPassword))
            throw new DomainException("New password is required.");

        if (newPassword.Length < 8 || newPassword.Length > 50)
            throw new DomainException("Password must be between 8 and 50 characters. BV-01.");

        if (string.IsNullOrWhiteSpace(confirmPassword))
            throw new DomainException("Confirm password is required.");

        if (newPassword != confirmPassword)
            throw new DomainException("Passwords do not match.");
    }
}
