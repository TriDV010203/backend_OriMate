using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _db;

    public TransactionRepository(AppDbContext db) => _db = db;

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAsync(Transaction transaction, CancellationToken ct = default)
    {
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken ct = default)
    {
        _db.Transactions.Update(transaction);
        await _db.SaveChangesAsync(ct);
    }

    public Task<decimal> GetConfirmedRevenueAsync(
        Guid creatorId,
        DateTime periodStartUtc,
        DateTime periodEndUtcExclusive,
        CancellationToken ct = default)
        => _db.Transactions
            .Where(t => t.CreatorId == creatorId
                     && t.Status == TransactionStatus.Confirmed
                     && t.ConfirmedAt >= periodStartUtc
                     && t.ConfirmedAt < periodEndUtcExclusive)
            .SumAsync(t => t.CreatorNetAmount, ct);

    public Task<decimal> GetTotalConfirmedRevenueAsync(Guid creatorId, CancellationToken ct = default)
        => _db.Transactions
            .Where(t => t.CreatorId == creatorId && t.Status == TransactionStatus.Confirmed)
            .SumAsync(t => t.CreatorNetAmount, ct);

    public Task<int> CountPendingByCreatorAsync(Guid creatorId, CancellationToken ct = default)
        => _db.Transactions
            .CountAsync(t => t.CreatorId == creatorId && t.Status == TransactionStatus.PendingConfirmation, ct);

    public async Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedAsync(
        TransactionStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.Transactions
            .Include(t => t.User).ThenInclude(u => u.Profile)
            .Include(t => t.Creator).ThenInclude(u => u!.Profile)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        query = query.OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<TransactionPlatformSummary> GetPlatformSummaryAsync(CancellationToken ct = default)
    {
        var confirmed = await _db.Transactions
            .Where(t => t.Status == TransactionStatus.Confirmed)
            .GroupBy(t => 1)
            .Select(g => new
            {
                GrossRevenue = g.Sum(t => t.Amount),
                Commission = g.Sum(t => t.PlatformFeeAmount),
                NetPaid = g.Sum(t => t.CreatorNetAmount),
                Count = g.Count()
            })
            .FirstOrDefaultAsync(ct);

        var pendingCount = await _db.Transactions.CountAsync(t => t.Status == TransactionStatus.PendingConfirmation, ct);
        var rejectedCount = await _db.Transactions.CountAsync(t => t.Status == TransactionStatus.Rejected, ct);

        return new TransactionPlatformSummary(
            confirmed?.GrossRevenue ?? 0m,
            confirmed?.Commission ?? 0m,
            confirmed?.NetPaid ?? 0m,
            confirmed?.Count ?? 0,
            pendingCount,
            rejectedCount);
    }
}
