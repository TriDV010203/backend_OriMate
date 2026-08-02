using OrigamiPlatform.Application.DTOs.Shop;

namespace OrigamiPlatform.Application.Commands.Shop;

public record UpdateShopLinkCommand(Guid ActorId, Guid ShopLinkId, UpdateShopLinkRequest Request);
