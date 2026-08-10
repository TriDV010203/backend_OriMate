using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.Tutorials;

public class GetTutorialBySlugHandler
{
    private readonly ITutorialRepository _tutorials;
    private readonly IVipSubscriptionRepository _vipSubscriptions;
    private readonly ILikeRepository _likes;
    private readonly IWishlistRepository _wishlists;
    private readonly IAchievementRepository? _achievements;
    private readonly ITutorialDifficultyRatingRepository? _ratings;
    private readonly ITutorialStepProgressRepository? _stepProgress;

    // The 3 rating/achievement/progress dependencies are optional (default null) purely so existing
    // callers that construct this handler directly with the original 4 args keep compiling; the DI
    // container always resolves and injects the real implementations in production (see
    // Infrastructure/DependencyInjection.cs). Every use below is null-guarded accordingly.
    public GetTutorialBySlugHandler(
        ITutorialRepository tutorials,
        IVipSubscriptionRepository vipSubscriptions,
        ILikeRepository likes,
        IWishlistRepository wishlists,
        IAchievementRepository? achievements = null,
        ITutorialDifficultyRatingRepository? ratings = null,
        ITutorialStepProgressRepository? stepProgress = null)
    {
        _tutorials = tutorials;
        _vipSubscriptions = vipSubscriptions;
        _likes = likes;
        _wishlists = wishlists;
        _achievements = achievements;
        _ratings = ratings;
        _stepProgress = stepProgress;
    }

    public async Task<TutorialDetailDto> HandleAsync(
        GetTutorialBySlugQuery query, CancellationToken ct = default)
    {
        var tutorial = await _tutorials.GetPublishedBySlugAsync(query.Slug, ct)
            ?? throw new NotFoundException($"Tutorial '{query.Slug}' not found.");

        var isVip = tutorial.Type == TutorialType.VIP;
        var hasAccess = false;

        if (isVip && query.CurrentUserId.HasValue)
            hasAccess = await _vipSubscriptions
                .HasActiveSubscriptionAsync(query.CurrentUserId.Value, tutorial.AuthorId, ct);

        var isVipLocked = isVip && !hasAccess;

        // BR-29: VIP tutorials show the first N steps free, the rest locked for non-subscribers
        var steps = tutorial.Steps
            .OrderBy(s => s.StepOrder)
            .Select(s => new TutorialStepDto(
                s.Id,
                s.StepOrder,
                isVipLocked && s.StepOrder > TutorialConstants.VipFreePreviewStepCount ? string.Empty : s.Description,
                isVipLocked && s.StepOrder > TutorialConstants.VipFreePreviewStepCount ? null : s.ImageUrl,
                IsLocked: isVipLocked && s.StepOrder > TutorialConstants.VipFreePreviewStepCount))
            .ToList();

        var likeCount = await _likes.GetLikeCountAsync(tutorial.Id, TargetType.Tutorial);
        var wishlistCount = await _wishlists.GetWishlistCountAsync(tutorial.Id, TargetType.Tutorial, ct);

        bool isLiked = false;
        bool isWishlisted = false;
        bool hasAchievement = false;
        bool hasRated = false;
        int completedStepCount = 0;

        if (query.CurrentUserId.HasValue && query.CurrentUserId.Value != Guid.Empty)
        {
            var likeRecord = await _likes.GetLikeAsync(query.CurrentUserId.Value, tutorial.Id, TargetType.Tutorial);
            isLiked = likeRecord != null;

            var wishlistRecord = await _wishlists.GetByUserAndTargetAsync(query.CurrentUserId.Value, tutorial.Id, TargetType.Tutorial, ct);
            isWishlisted = wishlistRecord != null;

            hasAchievement = _achievements is not null
                && await _achievements.GetByUserAndTutorialAsync(query.CurrentUserId.Value, tutorial.Id, ct) is not null;
            hasRated = _ratings is not null
                && await _ratings.ExistsAsync(query.CurrentUserId.Value, tutorial.Id, ct);
            completedStepCount = _stepProgress is not null
                ? (await _stepProgress.GetCompletedStepIdsAsync(query.CurrentUserId.Value, tutorial.Id, ct)).Count
                : 0;
        }

        var totalStepCount = tutorial.Steps.Count;
        var progressPercent = totalStepCount == 0
            ? 0
            : (int)Math.Round((double)completedStepCount / totalStepCount * 100);

        var ratingCounts = _ratings is not null
            ? await _ratings.GetCountsAsync(tutorial.Id, ct)
            : new Dictionary<PerceivedDifficulty, int>();
        var ratingSummary = new TutorialRatingSummaryDto(ratingCounts, ratingCounts.Values.Sum());

        var completedCount = _achievements is not null
            ? await _achievements.CountByTutorialAsync(tutorial.Id, ct)
            : 0;

        return new TutorialDetailDto(
            tutorial.Id,
            tutorial.Title,
            tutorial.Slug,
            tutorial.Description,
            tutorial.CoverImageUrl,
            tutorial.Type.ToString(),
            tutorial.Difficulty.ToString(),
            tutorial.CategoryId,
            tutorial.Category.Name,
            new AuthorDto(
                tutorial.Author.Id,
                tutorial.IsOfficial ? TutorialConstants.OfficialAuthorDisplayName : tutorial.Author.Profile?.DisplayName ?? tutorial.Author.Email,
                tutorial.IsOfficial ? null : tutorial.Author.Profile?.AvatarUrl),
            steps,
            tutorial.PublishedAt ?? tutorial.CreatedAt,
            IsVipLocked: isVipLocked,
            LikeCount: likeCount,
            WishlistCount: wishlistCount,
            IsLikedByCurrentUser: isLiked,
            IsWishlistedByCurrentUser: isWishlisted,
            MetaTitle: tutorial.MetaTitle,
            MetaDescription: tutorial.MetaDescription,
            Tags: tutorial.Tags,
            Model3DUrl: tutorial.Model3DUrl,
            Model3DPosterUrl: tutorial.Model3DPosterUrl,
            RatingSummary: ratingSummary,
            HasAchievement: hasAchievement,
            HasRated: hasRated,
            CompletedStepCount: completedStepCount,
            TotalStepCount: totalStepCount,
            ProgressPercent: progressPercent,
            CompletedCount: completedCount
        );
    }
}
