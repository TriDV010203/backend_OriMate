using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.DailyChallenge;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.DailyChallenge;

public class GetAdminChallengeCalendarHandler
{
    private readonly IDailyChallengeRepository _challenges;

    public GetAdminChallengeCalendarHandler(IDailyChallengeRepository challenges) => _challenges = challenges;

    public async Task<PagedResult<AdminChallengeCalendarItemDto>> HandleAsync(
        GetAdminChallengeCalendarQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var result = await _challenges.GetAllForAdminAsync(
            query.FromDate, query.ToDate, query.Status, page, pageSize, ct);

        var items = result.Items.Select(c => c.ToAdminCalendarDto()).ToList();
        return new PagedResult<AdminChallengeCalendarItemDto>(
            items, result.TotalCount, result.Page, result.PageSize, result.TotalPages);
    }
}
