using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Commands.Wishlists;

public record ToggleWishlistCommand(Guid UserId, Guid TargetId, TargetType TargetType);