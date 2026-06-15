using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Validators.Auth;

public static class ChangePasswordRequestValidator
{
    public static void Validate(string currentPassword, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword))
            throw new DomainException("Current password is required.");

        if (string.IsNullOrWhiteSpace(newPassword))
            throw new DomainException("New password is required.");

        if (newPassword.Length < 8 || newPassword.Length > 50)
            throw new DomainException("New password must be between 8 and 50 characters. BV-01.");

        if (string.IsNullOrWhiteSpace(confirmPassword))
            throw new DomainException("Confirm password is required.");

        if (newPassword != confirmPassword)
            throw new DomainException("Passwords do not match.");

        if (newPassword == currentPassword)
            throw new DomainException("New password must be different from the current password.");
    }
}
