using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.LearningPaths;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.LearningPaths;

public class GetLearningPathsHandler
{
    private readonly ILearningPathRepository _learningPathRepo;

    public GetLearningPathsHandler(ILearningPathRepository learningPathRepo) => _learningPathRepo = learningPathRepo;

    public async Task<PagedResult<LearningPathListItemDto>> HandleAsync(
        GetLearningPathsQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var result = await _learningPathRepo.GetPublishedAsync(query.Search, page, pageSize, ct);

        return new PagedResult<LearningPathListItemDto>(
            result.Items.Select(lp => lp.ToListItemDto()).ToList(),
            result.TotalCount, result.Page, result.PageSize, result.TotalPages);
    }
}
