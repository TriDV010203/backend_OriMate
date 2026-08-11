using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Commands.Likes;

public class ToggleLikeHandler
{
    private readonly ILikeRepository _likes;
    private readonly ICommunityPostRepository _posts;
    private readonly ITutorialRepository _tutorials;
    private readonly INotificationService _notifications;

    public ToggleLikeHandler(
        ILikeRepository likes,
        ICommunityPostRepository posts,
        ITutorialRepository tutorials,
        INotificationService notifications)
    {
        _likes = likes;
        _posts = posts;
        _tutorials = tutorials;
        _notifications = notifications;
    }

    public async Task<bool> HandleAsync(ToggleLikeCommand cmd, CancellationToken ct = default)
    {
        var existingLike = await _likes.GetLikeAsync(cmd.UserId, cmd.TargetId, cmd.TargetType);

        if (existingLike != null)
        {
            await _likes.RemoveAsync(existingLike);
            return false;
        }
        else
        {
            var newLike = new Like
            {
                UserId = cmd.UserId,
                TargetId = cmd.TargetId,
                TargetType = cmd.TargetType,
                CreatedAt = DateTime.UtcNow
            };

            await _likes.AddAsync(newLike);

            // Tìm tác giả bài viết/hướng dẫn để gửi thông báo
            Guid? authorId = null;
            if (cmd.TargetType == TargetType.CommunityPost)
            {
                var post = await _posts.GetByIdAsync(cmd.TargetId);
                if (post != null) authorId = post.AuthorId;
            }
            else if (cmd.TargetType == TargetType.Tutorial)
            {
                var tutorial = await _tutorials.GetByIdWithStepsAsync(cmd.TargetId, ct);
                if (tutorial != null) authorId = tutorial.AuthorId;
            }

            // Gửi thông báo nếu người like khác tác giả
            if (authorId.HasValue && authorId.Value != cmd.UserId)
            {
                await _notifications.NotifyUserAsync(
                    userId: authorId.Value,
                    type: NotificationType.NewLike,
                    message: "đã thích bài viết của bạn.",
                    entityType: cmd.TargetType.ToString(),
                    entityId: cmd.TargetId,
                    ct: ct
                );
            }

            return true;
        }
    }
}