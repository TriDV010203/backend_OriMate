using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Interfaces;

public interface ITutorialRepository
{
    Task<(IEnumerable<Tutorial> Items, int TotalCount)> GetPublishedAsync(
        string? search,
        int? categoryId,
        string? difficulty,
        TutorialType? type,
        int page,
        int pageSize,
        IReadOnlySet<Guid>? followedCreatorIds = null,
        CancellationToken ct = default);

    Task<Tutorial?> GetPublishedBySlugAsync(string slug, CancellationToken ct = default);
}
