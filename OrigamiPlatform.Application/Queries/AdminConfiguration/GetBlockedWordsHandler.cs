using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.AdminConfiguration;

public class GetBlockedWordsHandler
{
    private readonly IBlockedWordRepository _blockedWordRepo;

    public GetBlockedWordsHandler(IBlockedWordRepository blockedWordRepo) => _blockedWordRepo = blockedWordRepo;

    public async Task<List<BlockedWordResponse>> HandleAsync(GetBlockedWordsQuery query, CancellationToken ct = default)
    {
        var words = await _blockedWordRepo.GetAllAsync(ct);
        return words
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new BlockedWordResponse(w.Id, w.Word, w.CreatedAt))
            .ToList();
    }
}
