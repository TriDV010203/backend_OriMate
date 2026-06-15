namespace OrigamiPlatform.Application.Commands.Follows;

public record ToggleFollowCommand(Guid FollowerId, Guid FollowingId);