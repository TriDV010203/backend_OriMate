using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.Subscriptions;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Subscriptions;

public class ConfigureVipTierHandler
{
    private readonly ICreatorVipSettingsRepository _settings;
    private readonly ITutorialRepository _tutorials;

    public ConfigureVipTierHandler(ICreatorVipSettingsRepository settings, ITutorialRepository tutorials)
        => (_settings, _tutorials) = (settings, tutorials);

    public async Task<CreatorVipSettingsDto> HandleAsync(
        ConfigureVipTierCommand command,
        CancellationToken ct = default)
    {
        var existing = await _settings.GetByCreatorIdAsync(command.CreatorId, ct);

        // BR-VIP-06: enabling VIP selling requires at least MinPublishedTutorialsToSell Published tutorials.
        if (command.IsActive && existing?.IsActive != true)
        {
            var publishedCount = await _tutorials.CountPublishedByAuthorAsync(command.CreatorId, ct);
            if (publishedCount < VipConstants.MinPublishedTutorialsToSell)
                throw new DomainException(
                    $"You need at least {VipConstants.MinPublishedTutorialsToSell} published tutorials to enable VIP selling.");
        }

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
