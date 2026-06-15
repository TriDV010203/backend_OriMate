using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.Wishlists;

public record ToggleWishlistRequest(Guid TargetId, TargetType TargetType);
public record WishlistDto(
    Guid TargetId,
    TargetType TargetType,
    DateTime CreatedAt
);