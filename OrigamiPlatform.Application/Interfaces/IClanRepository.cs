using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IClanRepository
{
    Task<Clan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Clan?> GetByNameAsync(string name, CancellationToken ct = default);
    Task AddAsync(Clan clan, CancellationToken ct = default);
    Task UpdateAsync(Clan clan, CancellationToken ct = default);
}
