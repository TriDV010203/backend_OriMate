namespace OrigamiPlatform.Application.DTOs.CommunityPosts;

public record CommunityPostDto(
    Guid Id,
    Guid AuthorId,
    string Content,
    DateTime CreatedAt,
    int CommentCount,
    int LikeCount,
    bool IsLikedByCurrentUser,
    List<MediaItemDto> Media
);