using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Comments;

public class DeleteCommentHandler
{
    private readonly ICommentRepository _comments;

    public DeleteCommentHandler(ICommentRepository comments)
        => _comments = comments;

    public async Task HandleAsync(DeleteCommentCommand cmd, CancellationToken ct = default)
    {
        var comment = await _comments.GetByIdAsync(cmd.CommentId);
        if (comment == null)
            throw new DomainException("Comment not found.");

        if (comment.AuthorId != cmd.UserId)
            throw new ForbiddenException("You are not allowed to delete another user's comment.");

        if (DateTime.UtcNow - comment.CreatedAt > TimeSpan.FromMinutes(5))
            throw new DomainException("Comments can only be deleted within 5 minutes of posting.");

        // Do not hard-delete content — soft delete via IsDeleted (CLAUDE.md rule)
        comment.IsDeleted = true;
        comment.UpdatedAt = DateTime.UtcNow;

        await _comments.UpdateAsync(comment);
    }
}