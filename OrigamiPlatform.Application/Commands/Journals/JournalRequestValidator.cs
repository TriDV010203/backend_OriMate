using OrigamiPlatform.Application.DTOs.Journals;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Journals;

internal static class JournalRequestValidator
{
    private const int MaxContentLength = 2000;
    private const int MaxImageCount = 5;
    private const int MaxImageUrlLength = 512;
    private const int MaxStoredImageUrlsLength = 2000;

    public static (string Content, List<string> ImageUrls) Validate(
        string? content,
        List<string>? imageUrls)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Journal content must be between 1 and 2,000 characters.");

        var trimmedContent = content.Trim();
        if (trimmedContent.Length > MaxContentLength)
            throw new DomainException("Journal content must be between 1 and 2,000 characters.");

        var normalizedImageUrls = NormalizeImageUrls(imageUrls);
        var serializedImageUrls = JournalImageUrls.Serialize(normalizedImageUrls);

        if (serializedImageUrls is { Length: > MaxStoredImageUrlsLength })
            throw new DomainException($"Image URLs must not exceed {MaxStoredImageUrlsLength} characters in total.");

        return (trimmedContent, normalizedImageUrls);
    }

    private static List<string> NormalizeImageUrls(List<string>? imageUrls)
    {
        if (imageUrls is null)
            return [];

        if (imageUrls.Count > MaxImageCount)
            throw new DomainException($"A journal can have a maximum of {MaxImageCount} images.");

        var normalizedImageUrls = new List<string>();
        foreach (var imageUrl in imageUrls)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new DomainException("Image URLs must not be empty.");

            var trimmedImageUrl = imageUrl.Trim();
            if (trimmedImageUrl.Length > MaxImageUrlLength)
                throw new DomainException($"Each image URL must not exceed {MaxImageUrlLength} characters.");

            normalizedImageUrls.Add(trimmedImageUrl);
        }

        return normalizedImageUrls;
    }
}
