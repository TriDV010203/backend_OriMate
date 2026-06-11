using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Commands.Likes;
public record ToggleLikeCommand(Guid UserId, Guid TargetId, TargetType TargetType);