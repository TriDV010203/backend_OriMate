using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Interfaces;

public interface ITutorialRepository
{
    // Public browsing
    Task<(IEnumerable<Tutorial> Items, int TotalCount)> GetPublishedAsync(
        string? search,
        int? categoryId,
        string? difficulty,
        TutorialType? type,
        string sortBy,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Tutorial?> GetPublishedBySlugAsync(string slug, CancellationToken ct = default);

    // FT-04 authoring & review
    Task<Tutorial?> GetByIdWithStepsAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Tutorial>> GetByAuthorAsync(Guid authorId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<Tutorial>> GetPendingContributorReviewAsync(int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Tutorial tutorial, CancellationToken ct = default);
    Task UpdateAsync(Tutorial tutorial, CancellationToken ct = default);
    Task AddReviewHistoryAsync(TutorialReviewHistory history, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task<Category?> GetActiveCategoryAsync(int categoryId, CancellationToken ct = default);
    Task<CreatorVipSettings?> GetActiveCreatorVipSettingsAsync(Guid creatorId, CancellationToken ct = default);

    // FT-07 Edit-after-publish
    Task<Tutorial?> GetWorkingCopyByParentIdAsync(Guid parentId, CancellationToken ct = default);
    Task DeleteAsync(Guid tutorialId, CancellationToken ct = default);
    Task DeleteStepsByTutorialIdAsync(Guid tutorialId, CancellationToken ct = default);
    Task AddStepsAsync(IEnumerable<TutorialStep> steps, CancellationToken ct = default);
}
