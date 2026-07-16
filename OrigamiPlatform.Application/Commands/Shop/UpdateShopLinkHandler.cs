using OrigamiPlatform.Application.DTOs.Shop;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Application.Validators.Shop;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Shop;

public class UpdateShopLinkHandler
{
    private readonly IShopLinkRepository _shopLinks;
    private readonly IAuditLogRepository _auditLog;

    public UpdateShopLinkHandler(IShopLinkRepository shopLinks, IAuditLogRepository auditLog)
        => (_shopLinks, _auditLog) = (shopLinks, auditLog);

    public async Task<ShopLinkResponse> HandleAsync(UpdateShopLinkCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        UpdateShopLinkRequestValidator.Validate(req);

        var link = await _shopLinks.GetByIdAsync(command.ShopLinkId, ct)
            ?? throw new NotFoundException("Shop link not found.");

        var oldTitle = link.Title;

        link.Title = req.Title.Trim();
        link.Url = req.Url.Trim();
        link.ImageUrl = req.ImageUrl?.Trim();
        link.Category = req.Category?.Trim();
        link.IsActive = req.IsActive;
        link.UpdatedAt = DateTime.UtcNow;

        await _shopLinks.UpdateAsync(link, ct);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = command.ActorId,
            Action = "UpdateShopLink",
            EntityType = "ShopLink",
            EntityId = link.Id.ToString(),
            OldValue = oldTitle,
            NewValue = link.Title,
            CreatedAt = DateTime.UtcNow
        }, ct);

        return ToResponse(link);
    }

    private static ShopLinkResponse ToResponse(ShopLink link)
        => new(link.Id, link.Title, link.Url, link.ImageUrl, link.Category, link.IsActive, link.CreatedAt, link.UpdatedAt);
}
