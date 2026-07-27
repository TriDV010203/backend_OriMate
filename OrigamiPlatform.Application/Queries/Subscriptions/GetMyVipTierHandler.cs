using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.Subscriptions;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Subscriptions;

public class GetMyVipTierHandler
{
    private readonly ICreatorVipSettingsRepository _settings;

    public GetMyVipTierHandler(ICreatorVipSettingsRepository settings)
        => _settings = settings;

    public async Task<CreatorVipSettingsDto> HandleAsync(GetMyVipTierQuery query, CancellationToken ct = default)
    {
        var settings = await _settings.GetByCreatorIdAsync(query.CreatorId, ct);

        // Not configured yet — report the platform-fixed price with selling turned off.
        return settings?.ToDto()
            ?? new CreatorVipSettingsDto(Guid.Empty, query.CreatorId, VipConstants.FixedPriceVnd, false, DateTime.UtcNow, null);
    }
}
