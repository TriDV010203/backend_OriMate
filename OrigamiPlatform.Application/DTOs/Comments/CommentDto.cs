namespace OrigamiPlatform.Application.DTOs.Comments;

public record CommentDto(
    Guid Id,
    Guid UserId,
    string Content,
    DateTime CreatedAt,
    List<CommentDto>? Replies = null
);