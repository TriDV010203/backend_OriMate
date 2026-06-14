using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Comments;

public record AddCommentCommand(Guid UserId, Guid TargetId, TargetType TargetType, string Content);

public class AddCommentHandler
{
    private readonly ICommentRepository _comments;
    private readonly IBlockedWordRepository _blockedWords;

    public AddCommentHandler(ICommentRepository comments, IBlockedWordRepository blockedWords)
    {
        _comments = comments;
        _blockedWords = blockedWords;
    }

    public async Task<Guid> HandleAsync(AddCommentCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Content) || cmd.Content.Length > 500)
        {
            throw new DomainException("Comment length must be between 1 and 500 characters.");
        }

        var blockedWords = await _blockedWords.GetAllBlockedWordsAsync();
        var lowerContent = cmd.Content.ToLowerInvariant();

        foreach (var word in blockedWords)
        {
            if (lowerContent.Contains(word.ToLowerInvariant()))
            {
                throw new DomainException("Your comment contains blocked words and cannot be posted.");
            }
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

        // TODO ở Phase sau (Notifications): Gọi INotificationService để bắn thông báo cho Tác giả bài viết

        return comment.Id;
    }
}