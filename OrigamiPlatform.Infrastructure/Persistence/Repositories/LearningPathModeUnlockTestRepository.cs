using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class LearningPathModeUnlockTestRepository : ILearningPathModeUnlockTestRepository
{
    private readonly AppDbContext _db;

    public LearningPathModeUnlockTestRepository(AppDbContext db) => _db = db;

    public Task<LearningPathModeUnlockTest?> GetByModeIdAsync(Guid modeId, CancellationToken ct = default)
        => _db.LearningPathModeUnlockTests
            .Include(t => t.Tutorial)
            .FirstOrDefaultAsync(t => t.LearningPathModeId == modeId, ct);

    public async Task UpsertAsync(Guid modeId, Guid tutorialId, string? instructions, CancellationToken ct = default)
    {
        var existing = await _db.LearningPathModeUnlockTests
            .FirstOrDefaultAsync(t => t.LearningPathModeId == modeId, ct);

        if (existing is null)
        {
            _db.LearningPathModeUnlockTests.Add(new LearningPathModeUnlockTest
            {
                Id = Guid.NewGuid(),
                LearningPathModeId = modeId,
                TutorialId = tutorialId,
                Instructions = instructions,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.TutorialId = tutorialId;
            existing.Instructions = instructions;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
