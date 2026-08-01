namespace OrigamiPlatform.Application.DTOs.Shop;

public record PaperPatternDto(
    Guid Id,
    string Name,
    string? Description,
    string? ImageUrl,
    int PriceInHatGap,
    bool IsOwned
);
