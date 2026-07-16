using OrigamiPlatform.Application.DTOs.Shop;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Application.Validators.Shop;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Commands.Shop;

public class CreateShopLinkHandler
{
    private readonly IShopLinkRepository _shopLinks;
    private readonly IAuditLogRepository _auditLog;

    public CreateShopLinkHandler(IShopLinkRepository shopLinks, IAuditLogRepository auditLog)
        => (_shopLinks, _auditLog) = (shopLinks, auditLog);

    public async Task<ShopLinkResponse> HandleAsync(CreateShopLinkCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        CreateShopLinkRequestValidator.Validate(req);

        var link = new ShopLink
        {
            Id = Guid.NewGuid(),
            Title = req.Title.Trim(),
            Url = req.Url.Trim(),
            ImageUrl = req.ImageUrl?.Trim(),
            Category = req.Category?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _shopLinks.AddAsync(link, ct);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = command.ActorId,
            Action = "CreateShopLink",
            EntityType = "ShopLink",
            EntityId = link.Id.ToString(),
            OldValue = null,
            NewValue = link.Title,
            CreatedAt = DateTime.UtcNow
        }, ct);

        return ToResponse(link);
    }

    private static ShopLinkResponse ToResponse(ShopLink link)
        => new(link.Id, link.Title, link.Url, link.ImageUrl, link.Category, link.IsActive, link.CreatedAt, link.UpdatedAt);
}
