using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class WeeklyChallengeRepository : IWeeklyChallengeRepository
{
    private readonly AppDbContext _context;

    public WeeklyChallengeRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<WeeklyChallenge?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.WeeklyChallenges.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task UpdateAsync(WeeklyChallenge challenge, CancellationToken ct = default)
    {
        _context.WeeklyChallenges.Update(challenge);
        await _context.SaveChangesAsync(ct);
    }
}
