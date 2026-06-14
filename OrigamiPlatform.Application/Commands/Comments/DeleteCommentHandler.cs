using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Comments;

public record DeleteCommentCommand(Guid UserId, Guid CommentId);

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

        await _comments.RemoveAsync(comment);
    }
}