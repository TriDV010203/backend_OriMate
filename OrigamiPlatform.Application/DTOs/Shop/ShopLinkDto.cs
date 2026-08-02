namespace OrigamiPlatform.Application.DTOs.Shop;

public record ShopLinkDto(Guid Id, string Title, string Url, string? ImageUrl, string? Category, DateTime CreatedAt);
