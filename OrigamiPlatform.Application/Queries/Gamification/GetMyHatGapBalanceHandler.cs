using OrigamiPlatform.Application.DTOs.Gamification;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Gamification;

public class GetMyHatGapBalanceHandler
{
    private readonly IHatGapTransactionRepository _transactions;

    public GetMyHatGapBalanceHandler(IHatGapTransactionRepository transactions) => _transactions = transactions;

    public async Task<HatGapBalanceDto> HandleAsync(GetMyHatGapBalanceQuery query, CancellationToken ct = default)
    {
        var balance = await _transactions.GetLatestBalanceAsync(query.UserId, ct);
        return new HatGapBalanceDto(balance);
    }
}
