using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.Wishlists;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Application.DTOs.CommunityPosts;
using OrigamiPlatform.Application.DTOs.Tutorials;

namespace OrigamiPlatform.Application.Queries.Wishlists;

public class GetWishlistHandler
{
    private readonly IWishlistRepository _wishlists;
    private readonly ITutorialRepository _tutorials;
    private readonly ICommunityPostRepository _posts;
    private readonly IVipSubscriptionRepository _vipSubscriptions;
    private readonly ILikeRepository _likes;
    private readonly ICommentRepository _comments;

    public GetWishlistHandler(
        IWishlistRepository wishlists,
        ITutorialRepository tutorials,
        ICommunityPostRepository posts,
        IVipSubscriptionRepository vipSubscriptions,
        ILikeRepository likes,
        ICommentRepository comments)
    {
        _wishlists = wishlists;
        _tutorials = tutorials;
        _posts = posts;
        _vipSubscriptions = vipSubscriptions;
        _likes = likes;
        _comments = comments;
    }

    public async Task<PagedResult<WishlistDto>> HandleAsync(GetWishlistQuery query, CancellationToken ct = default)
    {
        var pagedData = await _wishlists.GetUserWishlistAsync(query.UserId, query.Type, query.Page, query.PageSize, ct);

        var tutorialIds = pagedData.Items.Where(w => w.TargetType == TargetType.Tutorial).Select(w => w.TargetId).ToList();
        var postIds = pagedData.Items.Where(w => w.TargetType == TargetType.CommunityPost).Select(w => w.TargetId).ToList();

        var tutorials = tutorialIds.Any() ? await _tutorials.GetByIdsAsync(tutorialIds, ct) : [];
        var posts = postIds.Any() ? await _posts.GetByIdsAsync(postIds, ct) : [];

        var subscribedCreatorIds = await _vipSubscriptions.GetSubscribedCreatorIdsAsync(query.UserId, ct);

        var likeCounts = new Dictionary<Guid, int>();
        var wishlistCounts = new Dictionary<Guid, int>();
        var commentCounts = new Dictionary<Guid, int>();
        var userLikedTutorialIds = new HashSet<Guid>();

        foreach (var t in tutorials)
        {
            likeCounts[t.Id] = await _likes.GetLikeCountAsync(t.Id, TargetType.Tutorial);
            wishlistCounts[t.Id] = await _wishlists.GetWishlistCountAsync(t.Id, TargetType.Tutorial, ct);
            commentCounts[t.Id] = await _comments.GetCommentCountAsync(t.Id, TargetType.Tutorial, ct);

            if (await _likes.GetLikeAsync(query.UserId, t.Id, TargetType.Tutorial) != null)
                userLikedTutorialIds.Add(t.Id);
        }

        var dtos = pagedData.Items.Select(w => {
            TutorialListItemDto? tutorialDto = null;
            WishlistPostDto? postDto = null;

            if (w.TargetType == TargetType.Tutorial)
            {
                var t = tutorials.FirstOrDefault(t => t.Id == w.TargetId);
                if (t != null)
                {
<<<<<<< HEAD
                    tutorialDto = new TutorialListItemDto(
                        t.Id,
                        t.Title,
                        t.Slug,
                        t.Description,
                        t.CoverImageUrl,
                        t.Type.ToString(),
                        t.Difficulty.ToString(),
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
                        IsLikedByCurrentUser: userLikedTutorialIds.Contains(t.Id),
                        IsWishlistedByCurrentUser: true
=======
                    tutorialDto = new WishlistTutorialDto(
                        Id: tut.Id,
                        Title: tut.Title,
                        Slug: tut.Slug,
                        Description: tut.Description,
                        CoverImageUrl: tut.CoverImageUrl,
                        Type: tut.Type.ToString(),
                        Difficulty: tut.Difficulty.ToString(),
                        CategoryId: tut.CategoryId,
                        CategoryName: tut.Category.Name,
                        Author: new AuthorDto(
                            tut.Author.Id,
                            tut.Author.Profile?.DisplayName ?? tut.Author.Email,
                            tut.Author.Profile?.AvatarUrl),
                        StepCount: tut.Steps.Count,
                        PublishedAt: tut.PublishedAt ?? tut.CreatedAt
>>>>>>> 81b6b5c (Fix logic errors and add some APIs.)
                    );
                }
            }
            else if (w.TargetType == TargetType.CommunityPost)
            {
                var post = posts.FirstOrDefault(p => p.Id == w.TargetId);
                if (post != null)
                {
                    postDto = new WishlistPostDto(

                        Content: post.Content,
                        Media: post.Media?.Select(m => new MediaItemDto
                        {
                            MediaUrl = m.Url,
                            MediaType = m.MediaType
                        }).ToList() ?? []
                    );
                }
            }

            return new WishlistDto(
                TargetId: w.TargetId,
                TargetType: w.TargetType,
                SavedAt: w.CreatedAt,
                Tutorial: tutorialDto,
                CommunityPost: postDto
            );
        }).ToList();

        return new PagedResult<WishlistDto>(dtos, pagedData.TotalCount, query.Page, query.PageSize, pagedData.TotalPages);
    }
}