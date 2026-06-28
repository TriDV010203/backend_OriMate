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
    private readonly ILikeRepository _likes;
    private readonly IWishlistRepository _wishlists;
    private readonly ICommentRepository _comments;

    public GetTutorialsHandler(
         ITutorialRepository tutorials,
         IVipSubscriptionRepository vipSubscriptions,
         IUserRepository users,
         ILikeRepository likes,
         IWishlistRepository wishlists,
         ICommentRepository comments)
    {
        _tutorials = tutorials;
        _vipSubscriptions = vipSubscriptions;
        _users = users;
        _likes = likes;
        _wishlists = wishlists;
        _comments = comments;
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

        HashSet<Guid>? followedIds = null;
        var subscribedCreatorIds = new HashSet<Guid>();

        if (query.CurrentUserId.HasValue)
        {
            var userId = query.CurrentUserId.Value;
            followedIds = await _users.GetFollowingIdsAsync(userId, ct);
            subscribedCreatorIds = await _vipSubscriptions.GetSubscribedCreatorIdsAsync(userId, ct);
        }

        var (items, totalCount) = await _tutorials.GetPublishedAsync(
            query.Search, query.CategoryId, query.Difficulty, type, sortBy, page, pageSize, followedIds, ct);

        var likeCounts = new Dictionary<Guid, int>();
        var wishlistCounts = new Dictionary<Guid, int>();
        var commentCounts = new Dictionary<Guid, int>();
        var userLikedPostIds = new HashSet<Guid>();
        var userWishlistedPostIds = new HashSet<Guid>();

        foreach (var t in items)
        {
            likeCounts[t.Id] = await _likes.GetLikeCountAsync(t.Id, TargetType.Tutorial);
            wishlistCounts[t.Id] = await _wishlists.GetWishlistCountAsync(t.Id, TargetType.Tutorial, ct);
            commentCounts[t.Id] = await _comments.GetCommentCountAsync(t.Id, TargetType.Tutorial, ct); 

            if (query.CurrentUserId.HasValue)
            {
                var userId = query.CurrentUserId.Value;

                if (await _likes.GetLikeAsync(userId, t.Id, TargetType.Tutorial) != null)
                    userLikedPostIds.Add(t.Id);

                if (await _wishlists.GetByUserAndTargetAsync(userId, t.Id, TargetType.Tutorial, ct) != null)
                    userWishlistedPostIds.Add(t.Id);
            }
        }

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
            IsVipLocked: t.Type == TutorialType.VIP && !subscribedCreatorIds.Contains(t.Author.Id),

            LikeCount: likeCounts.GetValueOrDefault(t.Id, 0),
            WishlistCount: wishlistCounts.GetValueOrDefault(t.Id, 0),
            CommentCount: commentCounts.GetValueOrDefault(t.Id, 0),
            IsLikedByCurrentUser: userLikedPostIds.Contains(t.Id),
            IsWishlistedByCurrentUser: userWishlistedPostIds.Contains(t.Id)
        )).ToList();

        return new PagedResult<TutorialListItemDto>(
            dtos,
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}
