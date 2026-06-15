using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Commands.Comments;

public record AddCommentCommand(Guid UserId, Guid TargetId, TargetType TargetType, string Content);