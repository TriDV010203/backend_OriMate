using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class LearningPathCompletionRepository : ILearningPathCompletionRepository
{
    private readonly AppDbContext _db;

    public LearningPathCompletionRepository(AppDbContext db) => _db = db;

    public Task<bool> ExistsAsync(Guid userId, Guid learningPathId, CancellationToken ct = default)
        => _db.LearningPathCompletions.AnyAsync(
            c => c.UserId == userId && c.LearningPathId == learningPathId, ct);

    public async Task AddAsync(LearningPathCompletion completion, CancellationToken ct = default)
    {
        _db.LearningPathCompletions.Add(completion);
        await _db.SaveChangesAsync(ct);
    }
}
