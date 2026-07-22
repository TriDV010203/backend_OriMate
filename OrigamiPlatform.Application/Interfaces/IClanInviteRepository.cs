using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IClanInviteRepository
{
    Task<ClanInvite?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ClanInvite>> GetPendingByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(ClanInvite invite, CancellationToken ct = default);
    Task UpdateAsync(ClanInvite invite, CancellationToken ct = default);
}
