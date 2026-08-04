using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class WeeklyChallengeSubmissionRepository : IWeeklyChallengeSubmissionRepository
{
    private readonly AppDbContext _context;

    public WeeklyChallengeSubmissionRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<WeeklyChallengeSubmission?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.WeeklyChallengeSubmissions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<List<WeeklyChallengeSubmission>> GetByChallengeAsync(Guid challengeId, CancellationToken ct = default)
        => _context.WeeklyChallengeSubmissions.Where(s => s.WeeklyChallengeId == challengeId).ToListAsync(ct);

    public async Task UpdateAsync(WeeklyChallengeSubmission submission, CancellationToken ct = default)
    {
        _context.WeeklyChallengeSubmissions.Update(submission);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateRangeAsync(IEnumerable<WeeklyChallengeSubmission> submissions, CancellationToken ct = default)
    {
        _context.WeeklyChallengeSubmissions.UpdateRange(submissions);
        await _context.SaveChangesAsync(ct);
    }
}
