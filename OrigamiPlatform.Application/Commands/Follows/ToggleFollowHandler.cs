using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Follows;

public record ToggleFollowCommand(Guid FollowerId, Guid FollowingId);

public class ToggleFollowHandler
{
    private readonly IFollowRepository _follows;

    public ToggleFollowHandler(IFollowRepository follows) => _follows = follows;

    public async Task<bool> HandleAsync(ToggleFollowCommand cmd, CancellationToken ct = default)
    {
        if (cmd.FollowerId == cmd.FollowingId)
        {
            throw new DomainException("You cannot follow yourself.");
        }

        var existing = await _follows.GetFollowAsync(cmd.FollowerId, cmd.FollowingId, ct);

        if (existing != null)
        {
            await _follows.RemoveAsync(existing, ct);
            return false;
        }
        else
        {
            var follow = new FollowRelationship
            {
                FollowerId = cmd.FollowerId,
                FollowingId = cmd.FollowingId,
                CreatedAt = DateTime.UtcNow
            };
            await _follows.AddAsync(follow, ct);
            return true;
        }
    }
}