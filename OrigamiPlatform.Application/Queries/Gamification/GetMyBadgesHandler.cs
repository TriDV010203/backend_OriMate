using OrigamiPlatform.Application.DTOs.Gamification;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Gamification;

public class GetMyBadgesHandler
{
    private readonly IUserBadgeRepository _userBadges;

    public GetMyBadgesHandler(IUserBadgeRepository userBadges) => _userBadges = userBadges;

    public async Task<List<UserBadgeDto>> HandleAsync(GetMyBadgesQuery query, CancellationToken ct = default)
    {
        var userBadges = await _userBadges.GetByUserAsync(query.UserId, ct);
        return userBadges
            .Select(ub => new UserBadgeDto(
                ub.BadgeId, ub.Badge.Code, ub.Badge.Name, ub.Badge.Description, ub.Badge.IconEmoji,
                ub.Badge.Category.ToString(), ub.EarnedAt))
            .ToList();
    }
}
