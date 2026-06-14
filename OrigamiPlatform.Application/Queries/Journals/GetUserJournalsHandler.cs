using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.Journals;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Journals;

public class GetUserJournalsHandler
{
    private readonly IJournalRepository _journals;

    public GetUserJournalsHandler(IJournalRepository journals)
        => _journals = journals;

    public async Task<PagedResult<JournalDto>> HandleAsync(
        GetUserJournalsQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var includePrivate = query.CurrentUserId == query.UserId;

        var (items, totalCount) = await _journals.GetByUserAsync(
            query.UserId,
            includePrivate,
            page,
            pageSize,
            ct);

        var dtos = items.Select(j => j.ToDto()).ToList();

        return new PagedResult<JournalDto>(
            dtos,
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}
