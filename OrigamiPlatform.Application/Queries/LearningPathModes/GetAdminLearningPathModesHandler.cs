using OrigamiPlatform.Application.DTOs.LearningPathModes;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.LearningPathModes;

public class GetAdminLearningPathModesHandler
{
    private readonly ILearningPathModeRepository _modes;
    private readonly ILearningPathModeUnlockTestRepository _unlockTests;

    public GetAdminLearningPathModesHandler(
        ILearningPathModeRepository modes, ILearningPathModeUnlockTestRepository unlockTests)
        => (_modes, _unlockTests) = (modes, unlockTests);

    public async Task<List<LearningPathModeAdminDto>> HandleAsync(
        GetAdminLearningPathModesQuery query, CancellationToken ct = default)
    {
        var modes = await _modes.GetAllAsync(includeInactive: true, ct);
        var result = new List<LearningPathModeAdminDto>();

        foreach (var mode in modes)
        {
            var pathCount = await _modes.CountPathsAsync(mode.Id, ct);
            var unlockTest = await _unlockTests.GetByModeIdAsync(mode.Id, ct);

            result.Add(new LearningPathModeAdminDto(
                mode.Id, mode.Name, mode.Description, mode.SortOrder, mode.IsActive,
                pathCount, unlockTest?.TutorialId, unlockTest?.Tutorial.Title, unlockTest?.Instructions,
                mode.CreatedAt, mode.UpdatedAt));
        }

        return result;
    }
}
