namespace OrigamiPlatform.Application.DTOs.Comments;

public record CommentDto(
    Guid Id,
    Guid UserId,
    string Content,
    DateTime CreatedAt,
    List<CommentDto>? Replies = null
);

// Kết quả phân trang bình luận. TotalCount chỉ tính bình luận gốc (dùng để phân trang / "Xem thêm"),
// còn TotalCommentCount tính cả trả lời lồng bên trong — dùng để hiển thị "Bình luận (N)".
public record CommentsPagedResult(
    IEnumerable<CommentDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    int TotalCommentCount
);