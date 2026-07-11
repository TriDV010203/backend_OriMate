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
            .SumAsync(t => t.Amount, ct);
}
