using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class FamilyProjectRepository : IFamilyProjectRepository
{
    private readonly AppDbContext _db;

    public FamilyProjectRepository(AppDbContext db) => _db = db;

    public Task<FamilyProject?> GetByIdWithMembersAsync(Guid projectId, CancellationToken ct = default)
        => _db.FamilyProjects
            .Include(p => p.Tutorial)
            .Include(p => p.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);

    public Task<int> CountActiveBySubscriptionAsync(Guid subscriptionId, CancellationToken ct = default)
        => _db.FamilyProjects
            .CountAsync(
                p => p.SubscriptionId == subscriptionId && p.Status == ProjectStatus.Active,
                ct);

    public Task<bool> IsFreePublishedTutorialAsync(Guid tutorialId, CancellationToken ct = default)
        => _db.Tutorials.AnyAsync(
            t => t.Id == tutorialId
                && t.Type == TutorialType.Free
                && t.Status == TutorialStatus.Published
                && !t.IsDeleted,
            ct);

    public async Task AddAsync(FamilyProject project, CancellationToken ct = default)
    {
        _db.FamilyProjects.Add(project);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddMemberAsync(FamilyProjectMember member, CancellationToken ct = default)
    {
        _db.FamilyProjectMembers.Add(member);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateMemberAsync(FamilyProjectMember member, CancellationToken ct = default)
    {
        _db.FamilyProjectMembers.Update(member);
        await _db.SaveChangesAsync(ct);
    }
}
