using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.WeeklyChallenge;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.WeeklyChallenge;

public class GetAdminWeeklyChallengeCalendarHandler
{
    private readonly IWeeklyChallengeRepository _challenges;

    public GetAdminWeeklyChallengeCalendarHandler(IWeeklyChallengeRepository challenges) => _challenges = challenges;

    public async Task<PagedResult<AdminWeeklyChallengeCalendarItemDto>> HandleAsync(
        GetAdminWeeklyChallengeCalendarQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var result = await _challenges.GetAllForAdminAsync(
            query.FromDate, query.ToDate, query.Status, page, pageSize, ct);

        var items = result.Items.Select(c => c.ToAdminCalendarDto()).ToList();
        return new PagedResult<AdminWeeklyChallengeCalendarItemDto>(
            items, result.TotalCount, result.Page, result.PageSize, result.TotalPages);
    }
}
