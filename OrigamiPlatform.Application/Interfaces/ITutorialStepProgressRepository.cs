using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface ITutorialStepProgressRepository
{
    /// <summary>The step with its parent tutorial loaded (to validate status), or null.</summary>
    Task<TutorialStep?> GetStepWithTutorialAsync(Guid stepId, CancellationToken ct = default);

    Task<bool> ExistsAsync(Guid userId, Guid stepId, CancellationToken ct = default);
    Task<TutorialStepProgress?> GetAsync(Guid userId, Guid stepId, CancellationToken ct = default);
    Task AddAsync(TutorialStepProgress progress, CancellationToken ct = default);
    Task RemoveAsync(TutorialStepProgress progress, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetCompletedStepIdsAsync(Guid userId, Guid tutorialId, CancellationToken ct = default);
    Task<int> CountStepsAsync(Guid tutorialId, CancellationToken ct = default);
    Task<bool> IsPublishedTutorialAsync(Guid tutorialId, CancellationToken ct = default);
}
