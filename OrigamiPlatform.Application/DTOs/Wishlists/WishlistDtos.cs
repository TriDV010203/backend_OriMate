using OrigamiPlatform.Application.DTOs.CommunityPosts;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.Wishlists;
public record WishlistDto(
    Guid TargetId,
    TargetType TargetType,
    DateTime SavedAt,
    TutorialListItemDto? Tutorial,
    WishlistPostDto? CommunityPost
);

public record WishlistPostDto(
    string Content,
    List<MediaItemDto> Media
);