namespace OrigamiPlatform.Application.DTOs.Shop;

public record CreateShopLinkRequest(string Title, string Url, string? ImageUrl, string? Category);
