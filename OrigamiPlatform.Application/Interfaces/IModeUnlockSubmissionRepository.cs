using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Interfaces;

public interface IModeUnlockSubmissionRepository
{
    Task<ModeUnlockSubmission?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Most recent submission by this user for this mode, if any (drives the "None/Pending/Approved/Rejected" state).
    Task<ModeUnlockSubmission?> GetLatestByUserAndModeAsync(Guid userId, Guid modeId, CancellationToken ct = default);

    Task<bool> ExistsApprovedAsync(Guid userId, Guid modeId, CancellationToken ct = default);

    Task AddAsync(ModeUnlockSubmission submission, CancellationToken ct = default);
    Task UpdateAsync(ModeUnlockSubmission submission, CancellationToken ct = default);

    Task<PagedResult<ModeUnlockSubmission>> GetPagedAsync(
        Guid? modeId, ModeUnlockSubmissionStatus? status, int page, int pageSize, CancellationToken ct = default);
}
