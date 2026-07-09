using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.Wishlists;

public record GetWishlistQuery(
    Guid UserId,
    TargetType? Type,
    int Page,
    int PageSize
);