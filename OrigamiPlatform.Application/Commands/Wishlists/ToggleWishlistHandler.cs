using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Commands.Wishlists;

public record ToggleWishlistCommand(Guid UserId, Guid TargetId, TargetType TargetType);

public class ToggleWishlistHandler
{
    private readonly IWishlistRepository _wishlists;

    public ToggleWishlistHandler(IWishlistRepository wishlists) => _wishlists = wishlists;

    public async Task<bool> HandleAsync(ToggleWishlistCommand cmd, CancellationToken ct = default)
    {
        var existing = await _wishlists.GetByUserAndTargetAsync(cmd.UserId, cmd.TargetId, cmd.TargetType, ct);

        if (existing != null)
        {
            await _wishlists.RemoveAsync(existing, ct);
            return false;
        }
        else
        {
            var wishlist = new Wishlist
            {
                UserId = cmd.UserId,
                TargetId = cmd.TargetId,
                TargetType = cmd.TargetType,
                CreatedAt = DateTime.UtcNow
            };
            await _wishlists.AddAsync(wishlist, ct);
            return true; 
        }
    }
}