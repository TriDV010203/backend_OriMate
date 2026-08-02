using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IStuckThreadRepository
{
    Task<StuckThread?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StuckThread?> GetByUserAndStepAsync(Guid userId, Guid stepId, CancellationToken ct = default);
    Task AddAsync(StuckThread thread, CancellationToken ct = default);
}
