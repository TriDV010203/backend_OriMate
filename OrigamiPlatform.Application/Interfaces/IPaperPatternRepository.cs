using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IPaperPatternRepository
{
    Task<List<PaperPattern>> GetActiveAsync(CancellationToken ct = default);
    Task<PaperPattern?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
