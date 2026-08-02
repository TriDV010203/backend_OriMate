using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class HatGapTransactionRepository : IHatGapTransactionRepository
{
    private readonly AppDbContext _db;

    public HatGapTransactionRepository(AppDbContext db) => _db = db;

    public async Task<int> GetLatestBalanceAsync(Guid userId, CancellationToken ct = default)
    {
        var latest = await _db.HatGapTransactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return latest?.BalanceAfter ?? 0;
    }

    public async Task<int> GetTotalEarnedAsync(Guid userId, CancellationToken ct = default)
        => await _db.HatGapTransactions
            .Where(t => t.UserId == userId && t.Type == HatGapTransactionType.Earn)
            .SumAsync(t => (int?)t.Amount, ct) ?? 0;

    public async Task AddAsync(HatGapTransaction transaction, CancellationToken ct = default)
    {
        _db.HatGapTransactions.Add(transaction);
        await _db.SaveChangesAsync(ct);
    }
}
