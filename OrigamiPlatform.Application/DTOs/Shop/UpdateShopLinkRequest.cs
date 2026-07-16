namespace OrigamiPlatform.Application.DTOs.Shop;

public record UpdateShopLinkRequest(string Title, string Url, string? ImageUrl, string? Category, bool IsActive);
