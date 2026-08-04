using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Shop;

public class PurchasePaperPatternHandler
{
    private readonly IPaperPatternRepository _patterns;
    private readonly IUserPaperPatternRepository _userPatterns;
    private readonly HatGapAwardService _hatGapAward;

    public PurchasePaperPatternHandler(
        IPaperPatternRepository patterns, IUserPaperPatternRepository userPatterns, HatGapAwardService hatGapAward)
        => (_patterns, _userPatterns, _hatGapAward) = (patterns, userPatterns, hatGapAward);

    public async Task HandleAsync(PurchasePaperPatternCommand command, CancellationToken ct = default)
    {
        var pattern = await _patterns.GetByIdAsync(command.PaperPatternId, ct);
        if (pattern is null || !pattern.IsActive)
            throw new NotFoundException($"Paper pattern {command.PaperPatternId} not found.");

        if (await _userPatterns.ExistsAsync(command.UserId, command.PaperPatternId, ct))
            throw new DomainException("You already own this pattern.");

        await _hatGapAward.AwardAsync(
            command.UserId, -pattern.PriceInHatGap, HatGapTransactionType.Spend,
            $"PaperPattern_{command.PaperPatternId}", ct);

        await _userPatterns.AddAsync(new UserPaperPattern
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            PaperPatternId = command.PaperPatternId,
            PurchasedAt = DateTime.UtcNow
        }, ct);
    }
}
