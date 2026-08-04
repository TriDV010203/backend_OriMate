using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.Tutorials;

public class GetRecommendedTutorialsHandler
{
    private readonly ITutorialRepository _tutorials;
    private readonly IAchievementRepository _achievements;
    private readonly IUserRepository _users;
    private readonly IVipSubscriptionRepository _vipSubscriptions;
    private readonly ILikeRepository _likes;
    private readonly IWishlistRepository _wishlists;
    private readonly ICommentRepository _comments;

    public GetRecommendedTutorialsHandler(
        ITutorialRepository tutorials,
        IAchievementRepository achievements,
        IUserRepository users,
        IVipSubscriptionRepository vipSubscriptions,
        ILikeRepository likes,
        IWishlistRepository wishlists,
        ICommentRepository comments)
    {
        _tutorials = tutorials;
        _achievements = achievements;
        _users = users;
        _vipSubscriptions = vipSubscriptions;
        _likes = likes;
        _wishlists = wishlists;
        _comments = comments;
    }

    public async Task<PagedResult<TutorialListItemDto>> HandleAsync(
        GetRecommendedTutorialsQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 20);

        // Default: no signed-in user, or a user with no completed tutorial yet — most popular
        // Beginner tutorials, no category bias (GetRecommendedAsync always orders by LikeCount desc).
        var difficulties = new[] { TutorialDifficulty.Beginner };
        var categoryIds = new List<int>();
        var excludeIds = new HashSet<Guid>();
        var subscribedCreatorIds = new HashSet<Guid>();

        if (query.UserId.HasValue)
        {
            var userId = query.UserId.Value;

            excludeIds = await _achievements.GetCompletedTutorialIdsAsync(userId, ct);
            var completedCategoryIds = await _achievements.GetCompletedCategoryIdsAsync(userId, ct);

            if (completedCategoryIds.Count > 0)
            {
                var user = await _users.GetByIdAsync(userId, ct);
                var skillLevel = user?.Profile?.SkillLevel ?? SkillLevel.Beginner;

                categoryIds = completedCategoryIds;
                difficulties = MapSkillLevelToDifficulties(skillLevel);
            }

            subscribedCreatorIds = await _vipSubscriptions.GetSubscribedCreatorIdsAsync(userId, ct);
        }

        var result = await _tutorials.GetRecommendedAsync(
            categoryIds, difficulties, excludeIds, page, pageSize, ct);

        var likeCounts = new Dictionary<Guid, int>();
        var wishlistCounts = new Dictionary<Guid, int>();
        var commentCounts = new Dictionary<Guid, int>();
        var userLikedPostIds = new HashSet<Guid>();
        var userWishlistedPostIds = new HashSet<Guid>();

        foreach (var t in result.Items)
        {
            likeCounts[t.Id] = await _likes.GetLikeCountAsync(t.Id, TargetType.Tutorial);
            wishlistCounts[t.Id] = await _wishlists.GetWishlistCountAsync(t.Id, TargetType.Tutorial, ct);
            commentCounts[t.Id] = await _comments.GetCommentCountAsync(t.Id, TargetType.Tutorial, ct);

            if (query.UserId.HasValue)
            {
                var userId = query.UserId.Value;

                if (await _likes.GetLikeAsync(userId, t.Id, TargetType.Tutorial) != null)
                    userLikedPostIds.Add(t.Id);

                if (await _wishlists.GetByUserAndTargetAsync(userId, t.Id, TargetType.Tutorial, ct) != null)
                    userWishlistedPostIds.Add(t.Id);
            }
        }

        var dtos = result.Items.Select(t => new TutorialListItemDto(
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
                t.IsOfficial ? TutorialConstants.OfficialAuthorDisplayName : t.Author.Profile?.DisplayName ?? t.Author.Email,
                t.IsOfficial ? null : t.Author.Profile?.AvatarUrl),
            t.Steps.Count,
            t.PublishedAt ?? t.CreatedAt,
            IsVipLocked: t.Type == TutorialType.VIP && !subscribedCreatorIds.Contains(t.Author.Id),

            LikeCount: likeCounts.GetValueOrDefault(t.Id, 0),
            WishlistCount: wishlistCounts.GetValueOrDefault(t.Id, 0),
            CommentCount: commentCounts.GetValueOrDefault(t.Id, 0),
            IsLikedByCurrentUser: userLikedPostIds.Contains(t.Id),
            IsWishlistedByCurrentUser: userWishlistedPostIds.Contains(t.Id)
        )).ToList();

        return new PagedResult<TutorialListItemDto>(
            dtos,
            result.TotalCount,
            page,
            pageSize,
            result.TotalPages);
    }

    // BR: Beginner user also sees some Intermediate; Advanced user only sees Advanced (no ceiling above Advanced).
    private static TutorialDifficulty[] MapSkillLevelToDifficulties(SkillLevel level) => level switch
    {
        SkillLevel.Beginner => new[] { TutorialDifficulty.Beginner, TutorialDifficulty.Intermediate },
        SkillLevel.Intermediate => new[] { TutorialDifficulty.Intermediate, TutorialDifficulty.Advanced },
        SkillLevel.Advanced => new[] { TutorialDifficulty.Advanced },
        _ => new[] { TutorialDifficulty.Beginner }
    };
}
