using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.Wishlists;

public record ToggleWishlistRequest(Guid TargetId, TargetType TargetType);