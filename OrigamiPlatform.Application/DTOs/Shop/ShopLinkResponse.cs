namespace OrigamiPlatform.Application.DTOs.Shop;

public record ShopLinkResponse(
    Guid Id,
    string Title,
    string Url,
    string? ImageUrl,
    string? Category,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
