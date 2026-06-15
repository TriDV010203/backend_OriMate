using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.Wishlists;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Wishlists;

public record GetMyWishlistQuery(Guid UserId, int Page, int PageSize);

public class GetMyWishlistHandler
{
    private readonly IWishlistRepository _wishlists;

    public GetMyWishlistHandler(IWishlistRepository wishlists) => _wishlists = wishlists;

    public async Task<PagedResult<WishlistDto>> HandleAsync(GetMyWishlistQuery query, CancellationToken ct = default)
    {
        var pagedData = await _wishlists.GetUserWishlistAsync(query.UserId, query.Page, query.PageSize, ct);

        var dtos = pagedData.Items.Select(w => new WishlistDto(
            w.TargetId,
            w.TargetType,
            w.CreatedAt
        )).ToList();

        return new PagedResult<WishlistDto>(dtos, pagedData.TotalCount, query.Page, query.PageSize, pagedData.TotalPages);
    }
}