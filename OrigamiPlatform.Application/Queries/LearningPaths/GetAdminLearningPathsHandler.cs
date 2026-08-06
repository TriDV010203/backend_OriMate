using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.LearningPaths;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.LearningPaths;

public class GetAdminLearningPathsHandler
{
    private readonly ILearningPathRepository _learningPathRepo;

    public GetAdminLearningPathsHandler(ILearningPathRepository learningPathRepo) => _learningPathRepo = learningPathRepo;

    public async Task<PagedResult<LearningPathListItemDto>> HandleAsync(
        GetAdminLearningPathsQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        LearningPathStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<LearningPathStatus>(query.Status, ignoreCase: true, out var parsed))
                throw new DomainException($"Invalid status '{query.Status}'.");
            status = parsed;
        }

        var result = await _learningPathRepo.GetAllForAdminAsync(query.Search, status, query.ModeId, page, pageSize, ct);

        return new PagedResult<LearningPathListItemDto>(
            result.Items.Select(lp => lp.ToListItemDto()).ToList(),
            result.TotalCount, result.Page, result.PageSize, result.TotalPages);
    }
}
