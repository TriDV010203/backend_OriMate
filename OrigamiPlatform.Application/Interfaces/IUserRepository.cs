using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByVerificationTokenAsync(string token, CancellationToken ct = default);
    Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken ct = default);
    Task<User?> GetByRefreshTokenHashAsync(string hashedToken, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetUsersByRoleAsync(UserRoleType role, CancellationToken ct = default);
}
