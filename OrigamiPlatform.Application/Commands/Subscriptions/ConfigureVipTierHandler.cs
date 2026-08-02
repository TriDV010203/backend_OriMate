using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.Subscriptions;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Commands.Subscriptions;

public class ConfigureVipTierHandler
{
    private readonly ICreatorVipSettingsRepository _settings;

    public ConfigureVipTierHandler(ICreatorVipSettingsRepository settings)
        => _settings = settings;

    public async Task<CreatorVipSettingsDto> HandleAsync(
        ConfigureVipTierCommand command,
        CancellationToken ct = default)
    {
        var existing = await _settings.GetByCreatorIdAsync(command.CreatorId, ct);

        // BR-VIP-05: price is platform-fixed — creators can only toggle IsActive.
        if (existing is null)
        {
            var settings = new CreatorVipSettings
            {
                Id = Guid.NewGuid(),
                CreatorId = command.CreatorId,
                Price = VipConstants.FixedPriceVnd,
                IsActive = command.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _settings.AddAsync(settings, ct);

            return settings.ToDto();
        }

        existing.Price = VipConstants.FixedPriceVnd;
        existing.IsActive = command.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _settings.UpdateAsync(existing, ct);

        return existing.ToDto();
    }
}
