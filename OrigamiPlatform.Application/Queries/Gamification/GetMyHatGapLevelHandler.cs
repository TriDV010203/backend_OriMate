using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.Gamification;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Gamification;

public class GetMyHatGapLevelHandler
{
    private readonly IHatGapTransactionRepository _transactions;

    public GetMyHatGapLevelHandler(IHatGapTransactionRepository transactions) => _transactions = transactions;

    public async Task<HatGapLevelDto> HandleAsync(GetMyHatGapLevelQuery query, CancellationToken ct = default)
    {
        var totalEarned = await _transactions.GetTotalEarnedAsync(query.UserId, ct);
        var balance = await _transactions.GetLatestBalanceAsync(query.UserId, ct);
        var progress = HatGapLevelCalculator.GetProgress(totalEarned);

        return new HatGapLevelDto(
            progress.Level,
            progress.TotalEarned,
            balance,
            progress.CurrentLevelFloor,
            progress.NextLevelThreshold,
            progress.HatGapToNextLevel,
            progress.ProgressPercent);
    }
}
