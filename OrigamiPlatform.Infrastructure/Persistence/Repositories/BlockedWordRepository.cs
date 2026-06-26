using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class BlockedWordRepository : IBlockedWordRepository
{
    private readonly AppDbContext _db;

    public BlockedWordRepository(AppDbContext db) => _db = db;

    public Task<List<BlockedWord>> GetAllAsync(CancellationToken ct = default)
        => _db.BlockedWords.OrderByDescending(w => w.CreatedAt).ToListAsync(ct);

    public Task<BlockedWord?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.BlockedWords.FirstOrDefaultAsync(w => w.Id == id, ct);

    public Task<bool> ExistsByWordAsync(string word, CancellationToken ct = default)
        => _db.BlockedWords.AnyAsync(w => w.Word == word.ToLower(), ct);

    public async Task<BlockedWord> AddAsync(BlockedWord word, CancellationToken ct = default)
    {
        _db.BlockedWords.Add(word);
        await _db.SaveChangesAsync(ct);
        return word;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var word = await _db.BlockedWords.FindAsync([id], ct);
        if (word is not null)
        {
            _db.BlockedWords.Remove(word);
            await _db.SaveChangesAsync(ct);
        }
    }
}
