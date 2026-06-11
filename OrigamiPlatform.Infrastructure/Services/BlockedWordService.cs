using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Infrastructure.Persistence;

namespace OrigamiPlatform.Infrastructure.Services;

public class BlockedWordService : IBlockedWordService
{
    private readonly AppDbContext _db;

    public BlockedWordService(AppDbContext db) => _db = db;

    public async Task<bool> ContainsBlockedWordAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        var blockedWords = await _db.BlockedWords
            .Select(b => b.Word)
            .ToListAsync(ct);

        return blockedWords.Any(w =>
            text.Contains(w, StringComparison.OrdinalIgnoreCase));
    }
}
