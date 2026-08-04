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
        TutorialDifficulty? difficulty,
        TutorialType? type,
        string sortBy,
        int page,
        int pageSize,
        IReadOnlySet<Guid>? followedCreatorIds = null,
        CancellationToken ct = default);

    Task<Tutorial?> GetPublishedBySlugAsync(string slug, CancellationToken ct = default);

    // FT-04 authoring & review
    Task<Tutorial?> GetByIdWithStepsAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Tutorial>> GetByAuthorAsync(Guid authorId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Tutorial tutorial, CancellationToken ct = default);
    Task UpdateAsync(Tutorial tutorial, CancellationToken ct = default);
    Task AddReviewHistoryAsync(TutorialReviewHistory history, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task<Category?> GetActiveCategoryAsync(int categoryId, CancellationToken ct = default);
    Task<CreatorVipSettings?> GetActiveCreatorVipSettingsAsync(Guid creatorId, CancellationToken ct = default);

    // FT-07 Edit-after-publish
    Task<Tutorial?> GetWorkingCopyByParentIdAsync(Guid parentId, CancellationToken ct = default);
    Task DeleteStepsByTutorialIdAsync(Guid tutorialId, CancellationToken ct = default);
    Task AddStepsAsync(IEnumerable<TutorialStep> steps, CancellationToken ct = default);

    Task<List<Tutorial>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    // Manager review queue
    Task<PagedResult<Tutorial>> GetPendingManagerReviewAsync(int page, int pageSize, CancellationToken ct = default);

    // Admin tutorial management (all main tutorials, any author, any status)
    Task<PagedResult<Tutorial>> GetAllForAdminAsync(
        string? search,
        TutorialStatus? status,
        int? categoryId,
        bool? isOfficial,
        int page,
        int pageSize,
        CancellationToken ct = default);

    // FT-31 AI Recommendation (rule-based)
    Task<PagedResult<Tutorial>> GetRecommendedAsync(
        IEnumerable<int> categoryIds,
        TutorialDifficulty[] difficulties,
        IEnumerable<Guid> excludeTutorialIds,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
