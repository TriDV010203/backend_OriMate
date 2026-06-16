using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Comments;

public class AddCommentHandler
{
    private readonly ICommentRepository _comments;
    private readonly IBlockedWordService _blockedWordService;
    private readonly INotificationService _notifications;
    private readonly ICommunityPostRepository _posts;
    private readonly ITutorialRepository _tutorials;

    public AddCommentHandler(
        ICommentRepository comments,
        IBlockedWordService blockedWordService,
        INotificationService notifications,
        ICommunityPostRepository posts,
        ITutorialRepository tutorials)
    {
        _comments = comments;
        _blockedWordService = blockedWordService;
        _notifications = notifications;
        _posts = posts;
        _tutorials = tutorials;
    }

    public async Task<Guid> HandleAsync(AddCommentCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Content) || cmd.Content.Length > 500)
        {
            throw new DomainException("Comment length must be between 1 and 500 characters.");
        }

        if (await _blockedWordService.ContainsBlockedWordAsync(cmd.Content, ct))
            throw new DomainException("Your comment contains blocked words and cannot be posted.");

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

        try
        {
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

            if (targetAuthorId.HasValue && targetAuthorId.Value != cmd.UserId)
            {
                await _notifications.NotifyUserAsync(
                    userId: targetAuthorId.Value,
                    type: NotificationType.System,
                    message: "Bài viết của bạn có bình luận mới.",
                    entityType: cmd.TargetType.ToString(),
                    entityId: cmd.TargetId,
                    ct: ct
                );
            }
        }
        catch
        {
            // notification failure must not affect the main flow
        }

        return comment.Id;
    }
}
