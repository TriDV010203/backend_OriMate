using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IPersonalMilestoneRepository
{
    Task<bool> ExistsAsync(Guid userId, int threshold, CancellationToken ct = default);
    Task<List<PersonalMilestone>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(PersonalMilestone milestone, CancellationToken ct = default);
}
