using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IBlockedWordRepository
{
    Task<List<BlockedWord>> GetAllAsync(CancellationToken ct = default);
    Task<BlockedWord?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsByWordAsync(string word, CancellationToken ct = default);
    Task<BlockedWord> AddAsync(BlockedWord word, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
