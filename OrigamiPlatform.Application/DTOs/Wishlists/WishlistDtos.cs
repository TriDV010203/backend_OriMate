using OrigamiPlatform.Application.DTOs.CommunityPosts;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.Wishlists;
public record WishlistDto(
    Guid TargetId,
    TargetType TargetType,
    DateTime SavedAt,
    WishlistTutorialDto? Tutorial,
    WishlistPostDto? CommunityPost
);

public record WishlistTutorialDto(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    string? CoverImageUrl,
    string Type,
    string? Difficulty,
    int CategoryId,
    string CategoryName,
    AuthorDto Author,
    int StepCount,
    DateTime PublishedAt
);

public record WishlistPostDto(
    string Content,
    List<MediaItemDto> Media
);