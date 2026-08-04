using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Tutorials;

public class GetVariantsHandler
{
    private readonly ITutorialVariantRepository _variantRepo;

    public GetVariantsHandler(ITutorialVariantRepository variantRepo) => _variantRepo = variantRepo;

    public async Task<List<TutorialVariantDto>> HandleAsync(GetVariantsQuery query, CancellationToken ct = default)
    {
        var variants = await _variantRepo.GetByParentIdAsync(query.ParentTutorialId, ct);

        return variants.Select(v => new TutorialVariantDto(
            v.VariantTutorialId,
            v.VariantTutorial.Title,
            v.VariantTutorial.Difficulty.ToString(),
            v.DifficultyDelta,
            v.VariantTutorial.Slug
        )).ToList();
    }
}
