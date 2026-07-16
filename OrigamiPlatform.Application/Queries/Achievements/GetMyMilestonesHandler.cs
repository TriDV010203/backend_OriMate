using OrigamiPlatform.Application.DTOs.Achievements;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Achievements;

public class GetMyMilestonesHandler
{
    private readonly IPersonalMilestoneRepository _milestones;

    public GetMyMilestonesHandler(IPersonalMilestoneRepository milestones) => _milestones = milestones;

    public async Task<List<PersonalMilestoneDto>> HandleAsync(GetMyMilestonesQuery query, CancellationToken ct = default)
    {
        var milestones = await _milestones.GetByUserAsync(query.UserId, ct);
        return milestones
            .OrderBy(m => m.Threshold)
            .Select(m => new PersonalMilestoneDto(m.Threshold, m.UnlockedAt))
            .ToList();
    }
}
