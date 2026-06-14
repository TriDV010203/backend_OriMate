using OrigamiPlatform.Application.DTOs.Comments;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.Comments;

public record GetCommentsQuery(Guid TargetId, TargetType TargetType, int Page, int PageSize);

public class GetCommentsHandler
{
    private readonly ICommentRepository _comments;

    public GetCommentsHandler(ICommentRepository comments)
        => _comments = comments;

    public async Task<PagedResult<CommentDto>> HandleAsync(GetCommentsQuery query, CancellationToken ct = default)
    {
        var pagedComments = await _comments.GetCommentsByTargetAsync(query.TargetId, query.TargetType, query.Page, query.PageSize);

        // Map từ Entity sang DTO
        var dtos = pagedComments.Items.Select(c => new CommentDto(
            c.Id,
            c.AuthorId,
            c.Content,
            c.CreatedAt
        )).ToList();

        return new PagedResult<CommentDto>(dtos, pagedComments.TotalCount, query.Page, query.PageSize, pagedComments.TotalPages);
    }
}