using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IClanMemberRepository
{
    Task<ClanMember?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<ClanMember>> GetByClanIdAsync(Guid clanId, CancellationToken ct = default);
    Task AddAsync(ClanMember member, CancellationToken ct = default);
    Task DeleteAsync(ClanMember member, CancellationToken ct = default);
}
