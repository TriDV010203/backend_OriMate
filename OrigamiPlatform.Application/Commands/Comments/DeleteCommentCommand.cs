namespace OrigamiPlatform.Application.Commands.Comments;

public record DeleteCommentCommand(Guid UserId, Guid CommentId);