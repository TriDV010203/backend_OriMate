using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface ILearningPathModeUnlockTestRepository
{
    Task<LearningPathModeUnlockTest?> GetByModeIdAsync(Guid modeId, CancellationToken ct = default);

    // Create if none exists for the mode yet, otherwise update TutorialId/Instructions in place.
    Task UpsertAsync(Guid modeId, Guid tutorialId, string? instructions, CancellationToken ct = default);
}
