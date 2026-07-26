using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.DailyChallenge;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.DailyChallenge;

public class GetChallengeSubmissionsHandler
{
    private readonly IDailyChallengeRepository _challenges;
    private readonly IDailyChallengeSubmissionRepository _submissions;
    private readonly ILikeRepository _likes;

    public GetChallengeSubmissionsHandler(
        IDailyChallengeRepository challenges,
        IDailyChallengeSubmissionRepository submissions,
        ILikeRepository likes)
        => (_challenges, _submissions, _likes) = (challenges, submissions, likes);

    public async Task<PagedResult<DailyChallengeSubmissionDto>> HandleAsync(
        GetChallengeSubmissionsQuery query, CancellationToken ct = default)
    {
        var challenge = await _challenges.GetByDateAsync(query.ChallengeDate, ct)
            ?? throw new NotFoundException("Không tìm thấy Thử thách ngày cho ngày này.");

        var submissions = await _submissions.GetByChallengeAsync(challenge.Id, ct);
        var submissionIds = submissions.Select(s => s.Id).ToList();

        var likeCounts = await _likes.GetCountsForTargetsAsync(submissionIds, TargetType.DailyChallengeSubmission);
        var likedByMe = query.CurrentUserId.HasValue
            ? await _likes.GetLikedTargetIdsAsync(query.CurrentUserId.Value, submissionIds, TargetType.DailyChallengeSubmission)
            : new HashSet<Guid>();

        var ranked = submissions
            .OrderByDescending(s => likeCounts.GetValueOrDefault(s.Id, 0))
            .ThenBy(s => s.CreatedAt)
            .ToList();

        var totalCount = ranked.Count;
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var items = ranked
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => s.ToDto(likeCounts.GetValueOrDefault(s.Id, 0), likedByMe.Contains(s.Id)))
            .ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<DailyChallengeSubmissionDto>(items, totalCount, page, pageSize, totalPages);
    }
}
