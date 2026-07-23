using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.Gamification;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Gamification;

public class PurchaseStreakFreezeHandler
{
    // BR-SEEDS-02: max 2 Freezes held at once; cost in Hạt Gấp per Freeze.
    private const int MaxFreezeCount = 2;
    private const int FreezeCostInHatGap = 20;

    private readonly IStreakLogRepository _streakLogs;
    private readonly HatGapAwardService _hatGap;

    public PurchaseStreakFreezeHandler(IStreakLogRepository streakLogs, HatGapAwardService hatGap)
        => (_streakLogs, _hatGap) = (streakLogs, hatGap);

    public async Task<StreakDto> HandleAsync(PurchaseStreakFreezeCommand command, CancellationToken ct = default)
    {
        var streak = await _streakLogs.GetByUserIdAsync(command.UserId, ct);

        if (streak.FreezeCount >= MaxFreezeCount)
            throw new DomainException($"You already have the maximum of {MaxFreezeCount} Streak Freezes.");

        await _hatGap.AwardAsync(
            command.UserId, -FreezeCostInHatGap, HatGapTransactionType.Spend, "StreakFreezePurchase", ct);

        streak.FreezeCount++;
        await _streakLogs.UpdateAsync(streak, ct);

        return new StreakDto(streak.CurrentStreak, streak.LongestStreak, streak.FreezeCount);
    }
}
