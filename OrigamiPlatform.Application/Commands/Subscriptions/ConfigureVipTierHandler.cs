using OrigamiPlatform.Application.DTOs.Subscriptions;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

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
        if (command.Price < 0)
            throw new DomainException("Price must not be negative.");

        var existing = await _settings.GetByCreatorIdAsync(command.CreatorId, ct);

        if (existing is null)
        {
            var settings = new CreatorVipSettings
            {
                Id = Guid.NewGuid(),
                CreatorId = command.CreatorId,
                Price = command.Price,
                IsActive = command.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _settings.AddAsync(settings, ct);

            return settings.ToDto();
        }

        existing.Price = command.Price;
        existing.IsActive = command.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _settings.UpdateAsync(existing, ct);

        return existing.ToDto();
    }
}
