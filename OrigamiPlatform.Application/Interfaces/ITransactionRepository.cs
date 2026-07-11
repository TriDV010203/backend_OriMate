using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(Transaction transaction, CancellationToken ct = default);

    Task UpdateAsync(Transaction transaction, CancellationToken ct = default);

    Task<decimal> GetConfirmedRevenueAsync(
        Guid creatorId,
        DateTime periodStartUtc,
        DateTime periodEndUtcExclusive,
        CancellationToken ct = default);
}
