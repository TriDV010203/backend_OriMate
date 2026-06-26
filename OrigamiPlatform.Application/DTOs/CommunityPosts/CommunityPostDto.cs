namespace OrigamiPlatform.Application.DTOs.CommunityPosts;

public record CommunityPostDto(
    Guid Id,
    Guid AuthorId,
    string Content,
    DateTime CreatedAt,
    int CommentCount,
    int LikeCount,
    bool IsLikedByCurrentUser,
    bool IsFromFollowedCreator,
    List<MediaItemDto> Media
);