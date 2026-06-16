using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IFamilyProjectRepository
{
    Task<FamilyProject?> GetByIdWithMembersAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>Counts active projects under a subscription (BR-28 / BV-27: max 5).</summary>
    Task<int> CountActiveBySubscriptionAsync(Guid subscriptionId, CancellationToken ct = default);

    /// <summary>True only when the tutorial exists, is Published, Free and not deleted (BR-28).</summary>
    Task<bool> IsFreePublishedTutorialAsync(Guid tutorialId, CancellationToken ct = default);

    Task AddAsync(FamilyProject project, CancellationToken ct = default);

    Task AddMemberAsync(FamilyProjectMember member, CancellationToken ct = default);
    Task UpdateMemberAsync(FamilyProjectMember member, CancellationToken ct = default);

    // FT-19 step progress
    Task<bool> StepBelongsToTutorialAsync(Guid stepId, Guid tutorialId, CancellationToken ct = default);
    Task<bool> StepProgressExistsAsync(Guid projectId, Guid stepId, Guid userId, CancellationToken ct = default);
    Task AddStepProgressAsync(FamilyProjectStepProgress progress, CancellationToken ct = default);
    Task<IReadOnlyList<TutorialStep>> GetTutorialStepsAsync(Guid tutorialId, CancellationToken ct = default);
    Task<IReadOnlyList<FamilyProjectStepProgress>> GetStepProgressesAsync(Guid projectId, CancellationToken ct = default);
}
