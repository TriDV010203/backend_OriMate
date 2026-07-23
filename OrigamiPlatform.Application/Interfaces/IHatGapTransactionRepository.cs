using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IHatGapTransactionRepository
{
    /// <summary>BalanceAfter of the user's most recent transaction, or 0 if none exists yet.</summary>
    Task<int> GetLatestBalanceAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(HatGapTransaction transaction, CancellationToken ct = default);
}
