using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.Tutorials;

public class GetTutorialsHandler
{
    private readonly ITutorialRepository _tutorials;
    private readonly IVipSubscriptionRepository _vipSubscriptions;
    private readonly IUserRepository _users;

    public GetTutorialsHandler(
         ITutorialRepository tutorials,
         IVipSubscriptionRepository vipSubscriptions,
         IUserRepository users)
    {
        _tutorials = tutorials;
        _vipSubscriptions = vipSubscriptions;
        _users = users;
    }

    public async Task<PagedResult<TutorialListItemDto>> HandleAsync(
        GetTutorialsQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 20);

        TutorialType? type = null;
        if (query.Type is not null)
        {
            if (!Enum.TryParse<TutorialType>(query.Type, ignoreCase: true, out var parsed))
                throw new DomainException($"Invalid type '{query.Type}'. Valid values: Free, VIP.");
            type = parsed;
        }

        var sortBy = query.SortBy?.ToLowerInvariant() ?? "date";
        if (sortBy is not ("date" or "likes"))
            throw new DomainException($"Invalid sortBy '{query.SortBy}'. Valid values: date, likes.");

        // Search endpoint is lenient: an unparseable difficulty yields an empty result set instead of a 400
        // (same spirit as an unknown CategoryId simply matching zero rows).
        TutorialDifficulty? difficulty = null;
        if (query.Difficulty is not null)
        {
            if (!Enum.TryParse<TutorialDifficulty>(query.Difficulty, ignoreCase: true, out var parsedDifficulty))
                return new PagedResult<TutorialListItemDto>(new List<TutorialListItemDto>(), 0, page, pageSize, 0);
            difficulty = parsedDifficulty;
        }

        HashSet<Guid>? followedIds = null;
        var subscribedCreatorIds = new HashSet<Guid>();

        if (query.CurrentUserId.HasValue)
        {
            var userId = query.CurrentUserId.Value;
            followedIds = await _users.GetFollowingIdsAsync(userId, ct);
            subscribedCreatorIds = await _vipSubscriptions.GetSubscribedCreatorIdsAsync(userId, ct);
        }

        var (dtos, totalCount) = await _tutorials.GetPublishedListAsync(
            query.Search, query.CategoryId, difficulty, type, sortBy, page, pageSize,
            followedIds, subscribedCreatorIds, query.CurrentUserId, ct);

        return new PagedResult<TutorialListItemDto>(
            dtos,
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}
