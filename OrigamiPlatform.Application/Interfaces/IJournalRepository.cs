using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IJournalRepository
{
    Task<Journal?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<(IEnumerable<Journal> Items, int TotalCount)> GetByUserAsync(
        Guid userId,
        bool includePrivate,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<bool> PublishedTutorialExistsAsync(Guid tutorialId, CancellationToken ct = default);

    Task AddAsync(Journal journal, CancellationToken ct = default);

    Task UpdateAsync(Journal journal, CancellationToken ct = default);

    Task DeleteAsync(Journal journal, CancellationToken ct = default);
}
