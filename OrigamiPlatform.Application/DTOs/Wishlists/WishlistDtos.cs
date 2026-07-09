using OrigamiPlatform.Application.DTOs.CommunityPosts;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.Wishlists;
public record WishlistDto(
    Guid TargetId,
    TargetType TargetType,
    DateTime CreatedAt,
    WishlistTutorialDto? Tutorial,
    WishlistPostDto? CommunityPost
);

public record WishlistTutorialDto(
    string Title,
    string? CoverImageUrl, 
    string Description
);

public record WishlistPostDto(
    string Content,
    List<MediaItemDto> Media
);