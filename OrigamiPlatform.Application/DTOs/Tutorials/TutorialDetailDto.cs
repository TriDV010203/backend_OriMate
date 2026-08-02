namespace OrigamiPlatform.Application.DTOs.Tutorials;

public record TutorialDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    string? CoverImageUrl,
    string Type,
    string Difficulty,
    int CategoryId,
    string CategoryName,
    AuthorDto Author,
    IEnumerable<TutorialStepDto> Steps,
    DateTime PublishedAt,
    bool IsVipLocked,
    int LikeCount,
    int WishlistCount,
    bool IsLikedByCurrentUser,
    bool IsWishlistedByCurrentUser,
    string? MetaTitle = null,
    string? MetaDescription = null,
    string? Tags = null
);
