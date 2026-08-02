using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class PersonalMilestoneRepository : IPersonalMilestoneRepository
{
    private readonly AppDbContext _db;

    public PersonalMilestoneRepository(AppDbContext db) => _db = db;

    public Task<bool> ExistsAsync(Guid userId, int threshold, CancellationToken ct = default)
        => _db.PersonalMilestones.AnyAsync(m => m.UserId == userId && m.Threshold == threshold, ct);

    public Task<List<PersonalMilestone>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => _db.PersonalMilestones.Where(m => m.UserId == userId).ToListAsync(ct);

    public async Task AddAsync(PersonalMilestone milestone, CancellationToken ct = default)
    {
        _db.PersonalMilestones.Add(milestone);
        await _db.SaveChangesAsync(ct);
    }
}
