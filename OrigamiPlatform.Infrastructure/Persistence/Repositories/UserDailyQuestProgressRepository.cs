using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class UserDailyQuestProgressRepository : IUserDailyQuestProgressRepository
{
    private readonly AppDbContext _db;

    public UserDailyQuestProgressRepository(AppDbContext db) => _db = db;

    public async Task<UserDailyQuestProgress> GetOrCreateAsync(
        Guid userId, Guid questId, DateOnly questDate, CancellationToken ct = default)
    {
        var existing = await GetAsync(userId, questId, questDate, ct);
        if (existing is not null)
            return existing;

        var created = new UserDailyQuestProgress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QuestId = questId,
            QuestDate = questDate
        };
        _db.UserDailyQuestProgresses.Add(created);
        await _db.SaveChangesAsync(ct);
        return created;
    }

    public Task<UserDailyQuestProgress?> GetAsync(
        Guid userId, Guid questId, DateOnly questDate, CancellationToken ct = default)
        => _db.UserDailyQuestProgresses.FirstOrDefaultAsync(
            p => p.UserId == userId && p.QuestId == questId && p.QuestDate == questDate, ct);

    public async Task UpdateAsync(UserDailyQuestProgress progress, CancellationToken ct = default)
    {
        _db.UserDailyQuestProgresses.Update(progress);
        await _db.SaveChangesAsync(ct);
    }
}
