using OrigamiPlatform.Application.DTOs.Shop;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Validators.Shop;

public static class CreateShopLinkRequestValidator
{
    public static void Validate(CreateShopLinkRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new DomainException("Title is required.");

        if (request.Title.Trim().Length > 200)
            throw new DomainException("Title must not exceed 200 characters.");

        if (string.IsNullOrWhiteSpace(request.Url))
            throw new DomainException("Url is required.");

        if (request.Url.Trim().Length > 500)
            throw new DomainException("Url must not exceed 500 characters.");

        if (request.Category != null && request.Category.Trim().Length > 100)
            throw new DomainException("Category must not exceed 100 characters.");
    }
}
