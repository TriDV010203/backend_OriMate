using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Users
            .Include(u => u.Roles)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _db.Users
            .Include(u => u.Roles)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => _db.Users.AnyAsync(u => u.Email == email, ct);

    public Task<User?> GetByVerificationTokenAsync(string token, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.VerificationToken == token, ct);

    public Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token, ct);

    public Task<User?> GetByRefreshTokenHashAsync(string hashedToken, CancellationToken ct = default)
        => _db.Users
            .Include(u => u.Roles)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.RefreshTokenHash == hashedToken, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<User>> GetUsersByRoleAsync(UserRoleType role, CancellationToken ct = default)
        => await _db.Users
            .Where(u => u.Roles.Any(r => r.Role == role))
            .ToListAsync(ct);
}
