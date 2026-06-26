using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class TutorialStepProgressRepository : ITutorialStepProgressRepository
{
    private readonly AppDbContext _db;

    public TutorialStepProgressRepository(AppDbContext db) => _db = db;

    public Task<TutorialStep?> GetStepWithTutorialAsync(Guid stepId, CancellationToken ct = default)
        => _db.TutorialSteps
            .Include(s => s.Tutorial)
            .FirstOrDefaultAsync(s => s.Id == stepId, ct);

    public Task<bool> ExistsAsync(Guid userId, Guid stepId, CancellationToken ct = default)
        => _db.TutorialStepProgresses.AnyAsync(p => p.UserId == userId && p.TutorialStepId == stepId, ct);

    public Task<TutorialStepProgress?> GetAsync(Guid userId, Guid stepId, CancellationToken ct = default)
        => _db.TutorialStepProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.TutorialStepId == stepId, ct);

    public async Task AddAsync(TutorialStepProgress progress, CancellationToken ct = default)
    {
        _db.TutorialStepProgresses.Add(progress);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(TutorialStepProgress progress, CancellationToken ct = default)
    {
        _db.TutorialStepProgresses.Remove(progress);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Guid>> GetCompletedStepIdsAsync(
        Guid userId, Guid tutorialId, CancellationToken ct = default)
        => await _db.TutorialStepProgresses
            .Where(p => p.UserId == userId && p.TutorialId == tutorialId)
            .Select(p => p.TutorialStepId)
            .ToListAsync(ct);

    public Task<int> CountStepsAsync(Guid tutorialId, CancellationToken ct = default)
        => _db.TutorialSteps.CountAsync(s => s.TutorialId == tutorialId, ct);

    public Task<bool> IsPublishedTutorialAsync(Guid tutorialId, CancellationToken ct = default)
        => _db.Tutorials.AnyAsync(
            t => t.Id == tutorialId && t.Status == TutorialStatus.Published && !t.IsDeleted, ct);
}
