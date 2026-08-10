using OrigamiPlatform.Application.DTOs.Comments;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Comments;

public class GetCommentsHandler
{
    private readonly ICommentRepository _comments;

    public GetCommentsHandler(ICommentRepository comments)
        => _comments = comments;

    public async Task<CommentsPagedResult> HandleAsync(GetCommentsQuery query, CancellationToken ct = default)
    {
        var pagedComments = await _comments.GetCommentsByTargetAsync(query.TargetId, query.TargetType, query.Page, query.PageSize);

        if (!pagedComments.Items.Any())
        {
            return new CommentsPagedResult(new List<CommentDto>(), 0, query.Page, query.PageSize, 0, 0);
        }

        var parentIds = pagedComments.Items.Select(c => c.Id).ToList();

        var allReplies = await _comments.GetRepliesByParentIdsAsync(parentIds);

        var dtos = pagedComments.Items.Select(c => new CommentDto(
            Id: c.Id,
            UserId: c.AuthorId,
            Content: c.Content,
            CreatedAt: c.CreatedAt,
            Replies: allReplies
                .Where(r => r.TargetId == c.Id)
                .Select(r => new CommentDto(
                    Id: r.Id,
                    UserId: r.AuthorId,
                    Content: r.Content,
                    CreatedAt: r.CreatedAt,
                    Replies: new List<CommentDto>()
                )).ToList()
        )).ToList();

        // Đếm tổng số bình luận gốc + trả lời (không chỉ trong trang hiện tại) để hiển thị "Bình luận (N)".
        var totalCommentCount = await _comments.GetCommentCountAsync(query.TargetId, query.TargetType, ct);

        return new CommentsPagedResult(dtos, pagedComments.TotalCount, query.Page, query.PageSize, pagedComments.TotalPages, totalCommentCount);
    }
}