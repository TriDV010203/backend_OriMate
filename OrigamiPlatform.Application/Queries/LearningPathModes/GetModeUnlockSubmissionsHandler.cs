using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.LearningPathModes;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.LearningPathModes;

public class GetModeUnlockSubmissionsHandler
{
    private readonly IModeUnlockSubmissionRepository _submissions;

    public GetModeUnlockSubmissionsHandler(IModeUnlockSubmissionRepository submissions) => _submissions = submissions;

    public async Task<PagedResult<ModeUnlockSubmissionDto>> HandleAsync(
        GetModeUnlockSubmissionsQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        ModeUnlockSubmissionStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<ModeUnlockSubmissionStatus>(query.Status, ignoreCase: true, out var parsed))
                throw new DomainException($"Invalid status '{query.Status}'.");
            status = parsed;
        }

        var result = await _submissions.GetPagedAsync(query.ModeId, status, page, pageSize, ct);

        return new PagedResult<ModeUnlockSubmissionDto>(
            result.Items.Select(s => s.ToDto()).ToList(),
            result.TotalCount, result.Page, result.PageSize, result.TotalPages);
    }
}
