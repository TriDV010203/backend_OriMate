using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Interfaces;

public record TransactionPlatformSummary(
    decimal TotalGrossRevenue,
    decimal TotalCommission,
    decimal TotalNetPaidToCreators,
    int ConfirmedCount,
    int PendingCount,
    int RejectedCount
);

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

    Task<decimal> GetTotalConfirmedRevenueAsync(Guid creatorId, CancellationToken ct = default);

    Task<int> CountPendingByCreatorAsync(Guid creatorId, CancellationToken ct = default);

    Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedAsync(
        TransactionStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<TransactionPlatformSummary> GetPlatformSummaryAsync(CancellationToken ct = default);
}
