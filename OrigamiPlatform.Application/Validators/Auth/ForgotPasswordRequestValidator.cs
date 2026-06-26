using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Validators.Auth;

public static class ForgotPasswordRequestValidator
{
    public static void Validate(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");

        if (!email.Contains('@') || !email.Contains('.'))
            throw new DomainException("A valid email address is required.");
    }
}
