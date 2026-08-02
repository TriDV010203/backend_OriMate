using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Common;

// FT-28: shared Hạt Gấp ledger writer — used by DailyQuest bonus, Advanced-tutorial reward
// (CompleteTutorialStepHandler) and Streak Freeze purchase (PurchaseStreakFreezeHandler).
public class HatGapAwardService
{
    private readonly IHatGapTransactionRepository _transactions;

    public HatGapAwardService(IHatGapTransactionRepository transactions) => _transactions = transactions;

    public async Task AwardAsync(
        Guid userId, int amount, HatGapTransactionType type, string source, CancellationToken ct = default)
    {
        var currentBalance = await _transactions.GetLatestBalanceAsync(userId, ct);
        var newBalance = currentBalance + amount;

        if (type == HatGapTransactionType.Spend && newBalance < 0)
            throw new DomainException("Insufficient Hạt Gấp balance.");

        await _transactions.AddAsync(new HatGapTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = amount,
            Type = type,
            Source = source,
            BalanceAfter = newBalance,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }
}
