using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IBadgeRepository
{
    Task<List<Badge>> GetAllActiveAsync(CancellationToken ct = default);
    Task<Badge?> GetByCodeAsync(string code, CancellationToken ct = default);
}
