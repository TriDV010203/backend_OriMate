using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Comments;

public class AddCommentHandler
{
    private readonly ICommentRepository _comments;
    private readonly INotificationService _notifications;
    private readonly ICommunityPostRepository _posts;
    private readonly ITutorialRepository _tutorials;
    private readonly IStuckThreadRepository _stuckThreads;

    public AddCommentHandler(
        ICommentRepository comments,
        INotificationService notifications,
        ICommunityPostRepository posts,
        ITutorialRepository tutorials,
        IStuckThreadRepository stuckThreads)
    {
        _comments = comments;
        _notifications = notifications;
        _posts = posts;
        _tutorials = tutorials;
        _stuckThreads = stuckThreads;
    }

    public async Task<Guid> HandleAsync(AddCommentCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Content) || cmd.Content.Length > 500)
        {
            throw new DomainException("Comment length must be between 1 and 500 characters.");
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            AuthorId = cmd.UserId,
            TargetId = cmd.TargetId,
            TargetType = cmd.TargetType,
            Content = cmd.Content,
            CreatedAt = DateTime.UtcNow
        };

        await _comments.AddAsync(comment);

        // Đã bỏ try-catch ở đây
        Guid? targetAuthorId = null;

        if (cmd.TargetType == TargetType.CommunityPost)
        {
            var post = await _posts.GetByIdAsync(cmd.TargetId);
            if (post != null) targetAuthorId = post.AuthorId;
        }
        else if (cmd.TargetType == TargetType.Tutorial)
        {
            var tutorial = await _tutorials.GetByIdWithStepsAsync(cmd.TargetId, ct);
            if (tutorial != null) targetAuthorId = tutorial.AuthorId;
        }
        else if (cmd.TargetType == TargetType.StuckThread)
        {
            var thread = await _stuckThreads.GetByIdAsync(cmd.TargetId, ct);
            if (thread != null) targetAuthorId = thread.UserId;
        }

        if (targetAuthorId.HasValue && targetAuthorId.Value != cmd.UserId)
        {
            await _notifications.NotifyUserAsync(
                userId: targetAuthorId.Value,
                type: NotificationType.NewComment, // (Có thể đổi thành NewComment theo Nhóm 2 sau)
                message: "Bài viết của bạn có bình luận mới.",
                entityType: cmd.TargetType.ToString(),
                entityId: cmd.TargetId,
                ct: ct
            );
        }

        return comment.Id;
    }
}
