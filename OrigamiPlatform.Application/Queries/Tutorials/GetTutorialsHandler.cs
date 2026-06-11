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
        => (_tutorials, _vipSubscriptions, _users) = (tutorials, vipSubscriptions, users);

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

        HashSet<Guid>? followedIds = null;
        var subscribedCreatorIds = new HashSet<Guid>();

        if (query.CurrentUserId.HasValue)
        {
            var userId = query.CurrentUserId.Value;
            followedIds = await _users.GetFollowingIdsAsync(userId, ct);
            subscribedCreatorIds = await _vipSubscriptions.GetSubscribedCreatorIdsAsync(userId, ct);
        }

        var (items, totalCount) = await _tutorials.GetPublishedAsync(
            query.Search, query.CategoryId, query.Difficulty, type, page, pageSize, followedIds, ct);

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
            t.PublishedAt!.Value,
            IsVipLocked: t.Type == TutorialType.VIP && !subscribedCreatorIds.Contains(t.Author.Id)
        )).ToList();

        return new PagedResult<TutorialListItemDto>(
            dtos,
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}
