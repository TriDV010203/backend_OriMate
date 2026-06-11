using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByVerificationTokenAsync(string token, CancellationToken ct = default);
    Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<HashSet<Guid>> GetFollowingIdsAsync(Guid userId, CancellationToken ct = default);
}
