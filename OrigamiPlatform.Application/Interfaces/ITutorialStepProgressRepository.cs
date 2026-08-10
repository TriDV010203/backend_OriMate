using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface ITutorialStepProgressRepository
{
    /// <summary>The step with its parent tutorial loaded (to validate status), or null.</summary>
    Task<TutorialStep?> GetStepWithTutorialAsync(Guid stepId, CancellationToken ct = default);

    /// <summary>True if an active (not soft-deleted) progress row exists for this step.</summary>
    Task<bool> ExistsAsync(Guid userId, Guid stepId, CancellationToken ct = default);

    /// <summary>Active (not soft-deleted) progress row, or null.</summary>
    Task<TutorialStepProgress?> GetAsync(Guid userId, Guid stepId, CancellationToken ct = default);

    /// <summary>Any progress row regardless of soft-delete state — used to detect a previously
    /// uncompleted row that should be reactivated (so HasBeenRewarded carries over) instead of inserted fresh.</summary>
    Task<TutorialStepProgress?> GetIncludingDeletedAsync(Guid userId, Guid stepId, CancellationToken ct = default);

    /// <summary>No-ops (does not throw) on a concurrent duplicate insert for the same (UserId, TutorialStepId) —
    /// the unique index already guarantees only one row ever exists.</summary>
    Task AddAsync(TutorialStepProgress progress, CancellationToken ct = default);

    /// <summary>Persists field changes on an existing row (reactivation, HasBeenRewarded).</summary>
    Task UpdateAsync(TutorialStepProgress progress, CancellationToken ct = default);

    /// <summary>Soft-delete: marks IsDeleted = true. Never resets HasBeenRewarded.</summary>
    Task RemoveAsync(TutorialStepProgress progress, CancellationToken ct = default);

    /// <summary>Completed (active, not soft-deleted) step ids for this user/tutorial.</summary>
    Task<IReadOnlyList<Guid>> GetCompletedStepIdsAsync(Guid userId, Guid tutorialId, CancellationToken ct = default);

    /// <summary>All step ids for the tutorial, ordered by StepOrder — read-only, never used to auto-complete.</summary>
    Task<IReadOnlyList<Guid>> GetOrderedStepIdsAsync(Guid tutorialId, CancellationToken ct = default);

    Task<int> CountStepsAsync(Guid tutorialId, CancellationToken ct = default);
    Task<bool> IsPublishedTutorialAsync(Guid tutorialId, CancellationToken ct = default);
}
