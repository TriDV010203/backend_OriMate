using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IFamilySubscriptionRepository
{
    /// <summary>
    /// Returns the user's currently active family subscription (Active and not expired),
    /// or null when none exists. The subscription is activated by the payment flow (FT-13);
    /// FT-18 only reads it.
    /// </summary>
    Task<FamilySubscription?> GetActiveByUserAsync(Guid userId, CancellationToken ct = default);
}
