using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Features.Tutorials.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.Tutorials;

/// <summary>Lists every main tutorial (any author, any status) for the admin management page,
/// so Admin/Manager can find and re-edit content regardless of who wrote it or its review state.</summary>
public class GetAdminTutorialsHandler
{
    private readonly ITutorialRepository _tutorialRepo;

    public GetAdminTutorialsHandler(ITutorialRepository tutorialRepo) => _tutorialRepo = tutorialRepo;

    public async Task<PagedResult<AdminTutorialListItemResponse>> HandleAsync(
        GetAdminTutorialsQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        TutorialStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<TutorialStatus>(query.Status, ignoreCase: true, out var parsed))
                throw new DomainException($"Invalid status '{query.Status}'.");
            status = parsed;
        }

        var result = await _tutorialRepo.GetAllForAdminAsync(
            query.Search, status, query.CategoryId, query.IsOfficial, page, pageSize, ct);

        var items = result.Items.Select(t => new AdminTutorialListItemResponse(
            t.Id,
            t.Title,
            t.Slug,
            t.CoverImageUrl,
            t.Type.ToString(),
            t.Difficulty.ToString(),
            t.Status.ToString(),
            t.CategoryId,
            t.Category.Name,
            t.Author.Profile?.DisplayName ?? t.Author.Email,
            t.IsOfficial,
            t.Steps.Count,
            t.CreatedAt,
            t.UpdatedAt,
            t.PublishedAt
        )).ToList();

        return new PagedResult<AdminTutorialListItemResponse>(
            items, result.TotalCount, result.Page, result.PageSize, result.TotalPages);
    }
}
