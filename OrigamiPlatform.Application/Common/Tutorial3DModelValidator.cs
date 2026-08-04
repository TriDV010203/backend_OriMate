using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Common;

public static class Tutorial3DModelValidator
{
    public const int MaxUrlLength = 512;

    public static void Validate(string? modelUrl, string? posterUrl)
    {
        var hasModel = !string.IsNullOrWhiteSpace(modelUrl);
        var hasPoster = !string.IsNullOrWhiteSpace(posterUrl);

        if (!hasModel && hasPoster)
            throw new DomainException("A 3D model poster cannot be set without a 3D model.");

        if (hasModel)
        {
            var modelUri = ValidateHttpsUrl(modelUrl!, "3D model URL");
            if (!modelUri.AbsolutePath.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
                throw new DomainException("3D model URL must point to a GLB file.");
        }

        if (hasPoster)
            ValidateHttpsUrl(posterUrl!, "3D model poster URL");
    }

    private static Uri ValidateHttpsUrl(string value, string fieldName)
    {
        if (value.Length > MaxUrlLength)
            throw new DomainException($"{fieldName} must not exceed {MaxUrlLength} characters.");

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new DomainException($"{fieldName} must be an absolute HTTPS URL.");
        }

        return uri;
    }
}
