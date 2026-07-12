namespace OrigamiPlatform.Application.Commands.Moderation;

public record DeleteViolatingCommentCommand(Guid ModeratorId, Guid CommentId, string Reason);
