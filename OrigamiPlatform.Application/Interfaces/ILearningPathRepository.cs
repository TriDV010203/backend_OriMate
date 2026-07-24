using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Interfaces;

public interface ILearningPathRepository
{
    // Public browsing — Published only
    Task<PagedResult<LearningPath>> GetPublishedAsync(
        string? search, int page, int pageSize, CancellationToken ct = default);

    Task<LearningPath?> GetPublishedByIdAsync(Guid id, CancellationToken ct = default);

    // Cross-link: which published path (if any) does this tutorial belong to.
    Task<LearningPath?> GetPublishedPathContainingTutorialAsync(Guid tutorialId, CancellationToken ct = default);

    // Admin management — any status
    Task<PagedResult<LearningPath>> GetAllForAdminAsync(
        string? search, LearningPathStatus? status, int page, int pageSize, CancellationToken ct = default);

    Task<LearningPath?> GetByIdForAdminAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(LearningPath learningPath, CancellationToken ct = default);
    Task UpdateAsync(LearningPath learningPath, CancellationToken ct = default);

    // Full replace of a path's items — same swap pattern as Tutorial steps on edit.
    Task ReplaceItemsAsync(Guid learningPathId, IEnumerable<LearningPathItem> items, CancellationToken ct = default);
}
