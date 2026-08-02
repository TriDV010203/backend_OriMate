using OrigamiPlatform.Application.DTOs.Shop;

namespace OrigamiPlatform.Application.Commands.Shop;

public record CreateShopLinkCommand(Guid ActorId, CreateShopLinkRequest Request);
