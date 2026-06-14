using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Features.Tutorials.DTOs;

namespace OrigamiPlatform.Application.Features.Tutorials.Services;

public interface ITutorialService
{
    Task<TutorialResponse> CreateDraftAsync(CreateTutorialRequest request, Guid authorId, CancellationToken ct = default);
    Task<TutorialResponse> SubmitForReviewAsync(Guid tutorialId, Guid authorId, CancellationToken ct = default);
    Task<PagedResult<TutorialListItemResponse>> GetMyTutorialsAsync(Guid authorId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<TutorialReviewItemResponse>> GetContributorQueueAsync(int page, int pageSize, CancellationToken ct = default);
    Task ContributorApproveAsync(Guid tutorialId, Guid reviewerId, CancellationToken ct = default);
    Task ContributorRequestRevisionAsync(Guid tutorialId, Guid reviewerId, string reason, CancellationToken ct = default);
    Task ManagerPublishAsync(Guid tutorialId, Guid managerId, CancellationToken ct = default);
    Task ManagerRejectAsync(Guid tutorialId, Guid managerId, string reason, CancellationToken ct = default);
    Task ManagerRemoveAsync(Guid tutorialId, Guid managerId, string? reason, CancellationToken ct = default);

    // FT-07 Edit-after-publish
    Task<WorkingCopyResponse> CreateWorkingCopyAsync(Guid tutorialId, Guid authorId, CancellationToken ct = default);
    Task<TutorialResponse> UpdateWorkingCopyAsync(Guid workingCopyId, UpdateTutorialRequest request, Guid authorId, CancellationToken ct = default);
    Task<TutorialResponse> SubmitEditAsync(Guid workingCopyId, Guid authorId, CancellationToken ct = default);
    Task ManagerApproveEditAsync(Guid workingCopyId, Guid managerId, CancellationToken ct = default);
    Task ManagerRejectEditAsync(Guid workingCopyId, Guid managerId, string reason, CancellationToken ct = default);
}
