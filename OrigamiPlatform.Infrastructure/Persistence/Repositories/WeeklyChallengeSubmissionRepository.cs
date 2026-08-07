using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class WeeklyChallengeSubmissionRepository : IWeeklyChallengeSubmissionRepository
{
    private readonly AppDbContext _db;

    public WeeklyChallengeSubmissionRepository(AppDbContext db) => _db = db;

    public Task<bool> ExistsAsync(Guid weeklyChallengeId, Guid userId, CancellationToken ct = default)
        => _db.WeeklyChallengeSubmissions.AnyAsync(
            s => s.WeeklyChallengeId == weeklyChallengeId && s.UserId == userId, ct);

    public async Task AddAsync(WeeklyChallengeSubmission submission, CancellationToken ct = default)
    {
        _db.WeeklyChallengeSubmissions.Add(submission);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(WeeklyChallengeSubmission submission, CancellationToken ct = default)
    {
        _db.WeeklyChallengeSubmissions.Update(submission);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateRangeAsync(IEnumerable<WeeklyChallengeSubmission> submissions, CancellationToken ct = default)
    {
        _db.WeeklyChallengeSubmissions.UpdateRange(submissions);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<WeeklyChallengeSubmission>> GetByChallengeAsync(Guid weeklyChallengeId, CancellationToken ct = default)
        => _db.WeeklyChallengeSubmissions
            .Include(s => s.User).ThenInclude(u => u.Profile)
            .Where(s => s.WeeklyChallengeId == weeklyChallengeId)
            .ToListAsync(ct);

    public Task<int> CountByUserWithMaxRankAsync(Guid userId, int maxRank, CancellationToken ct = default)
        => _db.WeeklyChallengeSubmissions.CountAsync(
            s => s.UserId == userId && s.FinalRank != null && s.FinalRank <= maxRank, ct);
}
