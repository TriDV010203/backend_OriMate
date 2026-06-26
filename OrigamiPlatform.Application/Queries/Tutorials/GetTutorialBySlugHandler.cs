//using OrigamiPlatform.Application.DTOs.Tutorials;
//using OrigamiPlatform.Application.Interfaces;
//using OrigamiPlatform.Domain.Exceptions;

//namespace OrigamiPlatform.Application.Queries.Tutorials;

//public class GetTutorialBySlugHandler
//{
//    private readonly ITutorialRepository _tutorials;

//    public GetTutorialBySlugHandler(ITutorialRepository tutorials) => _tutorials = tutorials;

//    public async Task<TutorialDetailDto> HandleAsync(
//        GetTutorialBySlugQuery query, CancellationToken ct = default)
//    {
//        var tutorial = await _tutorials.GetPublishedBySlugAsync(query.Slug, ct)
//            ?? throw new NotFoundException($"Tutorial '{query.Slug}' not found.");

//        var steps = tutorial.Steps
//            .OrderBy(s => s.StepOrder)
//            .Select(s => new TutorialStepDto(s.Id, s.StepOrder, s.Description, s.ImageUrl))
//            .ToList();

//        return new TutorialDetailDto(
//            tutorial.Id,
//            tutorial.Title,
//            tutorial.Slug,
//            tutorial.Description,
//            tutorial.CoverImageUrl,
//            tutorial.Type.ToString(),
//            tutorial.Difficulty,
//            tutorial.CategoryId,
//            tutorial.Category.Name,
//            new AuthorDto(
//                tutorial.Author.Id,
//                tutorial.Author.Profile?.DisplayName ?? tutorial.Author.Email,
//                tutorial.Author.Profile?.AvatarUrl),
//            steps,
//            tutorial.PublishedAt!.Value
//        );
//    }
//}
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.Tutorials;

public class GetTutorialBySlugHandler
{
    private readonly ITutorialRepository _tutorials;
    private readonly ILikeRepository _likes;
    private readonly IWishlistRepository _wishlists;

    public GetTutorialBySlugHandler(
        ITutorialRepository tutorials,
        ILikeRepository likes,
        IWishlistRepository wishlists)
    {
        _tutorials = tutorials;
        _likes = likes;
        _wishlists = wishlists;
    }

    public async Task<TutorialDetailDto> HandleAsync(
        GetTutorialBySlugQuery query, CancellationToken ct = default)
    {
        var tutorial = await _tutorials.GetPublishedBySlugAsync(query.Slug, ct)
            ?? throw new NotFoundException($"Tutorial '{query.Slug}' not found.");

        var steps = tutorial.Steps
            .OrderBy(s => s.StepOrder)
            .Select(s => new TutorialStepDto(s.Id, s.StepOrder, s.Description, s.ImageUrl))
            .ToList();

        var likeCount = await _likes.GetLikeCountAsync(tutorial.Id, TargetType.Tutorial);
        var wishlistCount = await _wishlists.GetWishlistCountAsync(tutorial.Id, TargetType.Tutorial, ct);

        bool isLiked = false;
        bool isWishlisted = false;

        if (query.CurrentUserId.HasValue && query.CurrentUserId.Value != Guid.Empty)
        {
            var likeRecord = await _likes.GetLikeAsync(query.CurrentUserId.Value, tutorial.Id, TargetType.Tutorial);
            isLiked = likeRecord != null;

            var wishlistRecord = await _wishlists.GetByUserAndTargetAsync(query.CurrentUserId.Value, tutorial.Id, TargetType.Tutorial, ct);
            isWishlisted = wishlistRecord != null;
        }

        return new TutorialDetailDto(
            tutorial.Id,
            tutorial.Title,
            tutorial.Slug,
            tutorial.Description,
            tutorial.CoverImageUrl,
            tutorial.Type.ToString(),
            tutorial.Difficulty,
            tutorial.CategoryId,
            tutorial.Category.Name,
            new AuthorDto(
                tutorial.Author.Id,
                tutorial.Author.Profile?.DisplayName ?? tutorial.Author.Email,
                tutorial.Author.Profile?.AvatarUrl),
            steps,
            tutorial.PublishedAt!.Value,

            likeCount,
            wishlistCount,
            isLiked,
            isWishlisted
        );
    }
}