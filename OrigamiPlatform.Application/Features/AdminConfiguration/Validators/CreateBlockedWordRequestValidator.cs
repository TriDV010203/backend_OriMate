using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Features.AdminConfiguration.Validators;

public static class CreateBlockedWordRequestValidator
{
    public static void Validate(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            throw new DomainException("Word is required.");

        if (word.Trim().Length > 50)
            throw new DomainException("Word must not exceed 50 characters.");
    }
}
