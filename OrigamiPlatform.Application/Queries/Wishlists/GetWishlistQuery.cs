namespace OrigamiPlatform.Application.Queries.Wishlists;

public record GetWishlistQuery(Guid UserId, int Page, int PageSize);