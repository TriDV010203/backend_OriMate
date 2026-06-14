using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Commands.Likes;

public class ToggleLikeHandler
{
    private readonly ILikeRepository _likes;

    public ToggleLikeHandler(ILikeRepository likes)
        => _likes = likes;

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

            return true;
        }
    }
}