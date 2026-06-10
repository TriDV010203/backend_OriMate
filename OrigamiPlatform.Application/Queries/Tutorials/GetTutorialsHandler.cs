using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.Tutorials;

public class GetTutorialsHandler
{
    private readonly ITutorialRepository _tutorials;

    public GetTutorialsHandler(ITutorialRepository tutorials) => _tutorials = tutorials;

    public async Task<PagedResult<TutorialListItemDto>> HandleAsync(
        GetTutorialsQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        TutorialType? type = null;
        if (query.Type is not null)
        {
            if (!Enum.TryParse<TutorialType>(query.Type, ignoreCase: true, out var parsed))
                throw new DomainException($"Invalid type '{query.Type}'. Valid values: Free, VIP.");
            type = parsed;
        }

        var (items, totalCount) = await _tutorials.GetPublishedAsync(
            query.Search, query.CategoryId, query.Difficulty, type, page, pageSize, ct);

        var dtos = items.Select(t => new TutorialListItemDto(
            t.Id,
            t.Title,
            t.Slug,
            t.Description,
            t.CoverImageUrl,
            t.Type.ToString(),
            t.Difficulty,
            t.CategoryId,
            t.Category.Name,
            new AuthorDto(
                t.Author.Id,
                t.Author.Profile?.DisplayName ?? t.Author.Email,
                t.Author.Profile?.AvatarUrl),
            t.Steps.Count,
            t.PublishedAt!.Value
        )).ToList();

        return new PagedResult<TutorialListItemDto>(
            dtos,
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}
