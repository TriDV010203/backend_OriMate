using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Moderation;

// FT-14 / BR-COMM-02: Contributor Reviewer removes a clearly violating comment directly, without going through the Report queue
public class DeleteViolatingCommentHandler
{
    private readonly ICommentRepository _comments;
    private readonly IAuditLogRepository _auditLog;

    public DeleteViolatingCommentHandler(ICommentRepository comments, IAuditLogRepository auditLog)
        => (_comments, _auditLog) = (comments, auditLog);

    public async Task HandleAsync(DeleteViolatingCommentCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length < 10)
            throw new DomainException("Reason must be at least 10 characters.");

        var comment = await _comments.GetByIdAsync(command.CommentId)
            ?? throw new NotFoundException($"Comment {command.CommentId} not found.");

        if (comment.IsDeleted)
            throw new DomainException("Comment already removed.");

        var originalContent = comment.Content;

        comment.IsDeleted = true;
        comment.UpdatedAt = DateTime.UtcNow;

        await _comments.UpdateAsync(comment);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = command.ModeratorId,
            Action = "RemoveViolatingComment",
            EntityType = "Comment",
            EntityId = comment.Id.ToString(),
            OldValue = originalContent,
            NewValue = null,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }
}
