using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class UserPaperPatternRepository : IUserPaperPatternRepository
{
    private readonly AppDbContext _db;

    public UserPaperPatternRepository(AppDbContext db) => _db = db;

    public Task<List<UserPaperPattern>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _db.UserPaperPatterns.Where(up => up.UserId == userId).ToListAsync(ct);

    public Task<bool> ExistsAsync(Guid userId, Guid patternId, CancellationToken ct = default)
        => _db.UserPaperPatterns.AnyAsync(up => up.UserId == userId && up.PaperPatternId == patternId, ct);

    public async Task AddAsync(UserPaperPattern userPaperPattern, CancellationToken ct = default)
    {
        _db.UserPaperPatterns.Add(userPaperPattern);
        await _db.SaveChangesAsync(ct);
    }
}
