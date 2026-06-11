using OrigamiPlatform.Application.DTOs.Achievements;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Achievements;

public class GetUserAchievementsHandler
{
    private readonly IAchievementRepository _achievements;

    public GetUserAchievementsHandler(IAchievementRepository achievements)
        => _achievements = achievements;

    public async Task<PagedResult<AchievementDto>> HandleAsync(
        GetUserAchievementsQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var includePrivate = query.CurrentUserId == query.UserId;

        var (items, totalCount) = await _achievements.GetByUserAsync(
            query.UserId,
            includePrivate,
            page,
            pageSize,
            ct);

        var dtos = items.Select(a => a.ToDto()).ToList();

        return new PagedResult<AchievementDto>(
            dtos,
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}
