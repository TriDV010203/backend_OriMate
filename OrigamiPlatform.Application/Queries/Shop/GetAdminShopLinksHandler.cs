using OrigamiPlatform.Application.DTOs.Shop;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Shop;

public class GetAdminShopLinksHandler
{
    private readonly IShopLinkRepository _shopLinks;

    public GetAdminShopLinksHandler(IShopLinkRepository shopLinks) => _shopLinks = shopLinks;

    public async Task<List<ShopLinkResponse>> HandleAsync(GetAdminShopLinksQuery query, CancellationToken ct = default)
    {
        var links = await _shopLinks.GetAllAsync(ct);
        return links
            .Select(l => new ShopLinkResponse(l.Id, l.Title, l.Url, l.ImageUrl, l.Category, l.IsActive, l.CreatedAt, l.UpdatedAt))
            .ToList();
    }
}
