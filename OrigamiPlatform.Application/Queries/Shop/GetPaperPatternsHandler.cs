using OrigamiPlatform.Application.DTOs.Shop;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Shop;

public class GetPaperPatternsHandler
{
    private readonly IPaperPatternRepository _patterns;
    private readonly IUserPaperPatternRepository _userPatterns;

    public GetPaperPatternsHandler(IPaperPatternRepository patterns, IUserPaperPatternRepository userPatterns)
        => (_patterns, _userPatterns) = (patterns, userPatterns);

    public async Task<List<PaperPatternDto>> HandleAsync(GetPaperPatternsQuery query, CancellationToken ct = default)
    {
        var patterns = await _patterns.GetActiveAsync(ct);
        var owned = await _userPatterns.GetByUserIdAsync(query.UserId, ct);
        var ownedIds = owned.Select(o => o.PaperPatternId).ToHashSet();

        return patterns.Select(p => new PaperPatternDto(
            p.Id,
            p.Name,
            p.Description,
            p.ImageUrl,
            p.PriceInHatGap,
            ownedIds.Contains(p.Id)
        )).ToList();
    }
}
