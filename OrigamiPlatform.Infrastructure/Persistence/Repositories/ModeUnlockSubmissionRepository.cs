using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class ModeUnlockSubmissionRepository : IModeUnlockSubmissionRepository
{
    private readonly AppDbContext _db;

    public ModeUnlockSubmissionRepository(AppDbContext db) => _db = db;

    private IQueryable<ModeUnlockSubmission> BaseQuery() => _db.ModeUnlockSubmissions
        .Include(s => s.User).ThenInclude(u => u.Profile)
        .Include(s => s.LearningPathMode)
        .Include(s => s.Tutorial);

    public Task<ModeUnlockSubmission?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => BaseQuery().FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<ModeUnlockSubmission?> GetLatestByUserAndModeAsync(Guid userId, Guid modeId, CancellationToken ct = default)
        => _db.ModeUnlockSubmissions
            .Where(s => s.UserId == userId && s.LearningPathModeId == modeId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public Task<bool> ExistsApprovedAsync(Guid userId, Guid modeId, CancellationToken ct = default)
        => _db.ModeUnlockSubmissions.AnyAsync(
            s => s.UserId == userId && s.LearningPathModeId == modeId && s.Status == ModeUnlockSubmissionStatus.Approved, ct);

    public async Task AddAsync(ModeUnlockSubmission submission, CancellationToken ct = default)
    {
        _db.ModeUnlockSubmissions.Add(submission);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ModeUnlockSubmission submission, CancellationToken ct = default)
    {
        _db.ModeUnlockSubmissions.Update(submission);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<ModeUnlockSubmission>> GetPagedAsync(
        Guid? modeId, ModeUnlockSubmissionStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = BaseQuery().AsQueryable();

        if (modeId.HasValue)
            query = query.Where(s => s.LearningPathModeId == modeId.Value);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ModeUnlockSubmission>(
            items, totalCount, page, pageSize, (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}
