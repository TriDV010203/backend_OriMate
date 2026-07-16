using OrigamiPlatform.Application.DTOs.Shop;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Shop;

public class GetShopLinksHandler
{
    private readonly IShopLinkRepository _shopLinks;

    public GetShopLinksHandler(IShopLinkRepository shopLinks) => _shopLinks = shopLinks;

    public async Task<List<ShopLinkDto>> HandleAsync(GetShopLinksQuery query, CancellationToken ct = default)
    {
        var links = await _shopLinks.GetActiveAsync(ct);
        return links
            .Select(l => new ShopLinkDto(l.Id, l.Title, l.Url, l.ImageUrl, l.Category, l.CreatedAt))
            .ToList();
    }
}
