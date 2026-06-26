using System.Text.Json;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.DTOs.Journals;

public static class JournalMapping
{
    public static JournalDto ToDto(this Journal journal)
        => new(
            journal.Id,
            journal.UserId,
            journal.LinkedTutorialId,
            journal.LinkedTutorial?.Title,
            journal.LinkedTutorial?.Slug,
            journal.Content,
            JournalImageUrls.Deserialize(journal.ImageUrls),
            journal.IsPublic,
            journal.CreatedAt,
            journal.UpdatedAt);
}

internal static class JournalImageUrls
{
    public static string? Serialize(IEnumerable<string>? imageUrls)
    {
        var normalized = Normalize(imageUrls);
        return normalized.Count == 0 ? null : JsonSerializer.Serialize(normalized);
    }

    public static List<string> Deserialize(string? imageUrls)
    {
        if (string.IsNullOrWhiteSpace(imageUrls))
            return [];

        try
        {
            return Normalize(JsonSerializer.Deserialize<List<string>>(imageUrls));
        }
        catch (JsonException)
        {
            return Normalize(imageUrls.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
    }

    private static List<string> Normalize(IEnumerable<string>? imageUrls)
        => imageUrls?
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .ToList() ?? [];
}
