using OrigamiPlatform.Application.DTOs.Gamification;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Gamification;

public class GetBadgeCatalogHandler
{
    private readonly IBadgeRepository _badges;

    public GetBadgeCatalogHandler(IBadgeRepository badges) => _badges = badges;

    public async Task<List<BadgeDto>> HandleAsync(GetBadgeCatalogQuery query, CancellationToken ct = default)
    {
        var badges = await _badges.GetAllActiveAsync(ct);
        return badges
            .Select(b => new BadgeDto(b.Id, b.Code, b.Name, b.Description, b.IconEmoji, b.Category.ToString(), b.Threshold))
            .ToList();
    }
}
