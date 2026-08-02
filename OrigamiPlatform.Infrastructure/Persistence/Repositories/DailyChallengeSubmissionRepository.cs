using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class DailyChallengeSubmissionRepository : IDailyChallengeSubmissionRepository
{
    private readonly AppDbContext _db;

    public DailyChallengeSubmissionRepository(AppDbContext db) => _db = db;

    public Task<bool> ExistsAsync(Guid dailyChallengeId, Guid userId, CancellationToken ct = default)
        => _db.DailyChallengeSubmissions.AnyAsync(
            s => s.DailyChallengeId == dailyChallengeId && s.UserId == userId, ct);

    public async Task AddAsync(DailyChallengeSubmission submission, CancellationToken ct = default)
    {
        _db.DailyChallengeSubmissions.Add(submission);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(DailyChallengeSubmission submission, CancellationToken ct = default)
    {
        _db.DailyChallengeSubmissions.Update(submission);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateRangeAsync(IEnumerable<DailyChallengeSubmission> submissions, CancellationToken ct = default)
    {
        _db.DailyChallengeSubmissions.UpdateRange(submissions);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<DailyChallengeSubmission>> GetByChallengeAsync(Guid dailyChallengeId, CancellationToken ct = default)
        => _db.DailyChallengeSubmissions
            .Include(s => s.User).ThenInclude(u => u.Profile)
            .Where(s => s.DailyChallengeId == dailyChallengeId)
            .ToListAsync(ct);

    public Task<int> CountByUserWithMaxRankAsync(Guid userId, int maxRank, CancellationToken ct = default)
        => _db.DailyChallengeSubmissions.CountAsync(
            s => s.UserId == userId && s.FinalRank != null && s.FinalRank <= maxRank, ct);
}
