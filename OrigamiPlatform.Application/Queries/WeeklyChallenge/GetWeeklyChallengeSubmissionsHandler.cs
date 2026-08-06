using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.WeeklyChallenge;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.WeeklyChallenge;

public class GetWeeklyChallengeSubmissionsHandler
{
    private readonly IWeeklyChallengeRepository _challenges;
    private readonly IWeeklyChallengeSubmissionRepository _submissions;
    private readonly ILikeRepository _likes;

    public GetWeeklyChallengeSubmissionsHandler(
        IWeeklyChallengeRepository challenges,
        IWeeklyChallengeSubmissionRepository submissions,
        ILikeRepository likes)
        => (_challenges, _submissions, _likes) = (challenges, submissions, likes);

    public async Task<PagedResult<WeeklyChallengeSubmissionDto>> HandleAsync(
        GetWeeklyChallengeSubmissionsQuery query, CancellationToken ct = default)
    {
        var challenge = await _challenges.GetByDateAsync(query.ChallengeDate, ct)
            ?? throw new NotFoundException("Không tìm thấy Thử thách tuần cho ngày này.");

        var submissions = await _submissions.GetByChallengeAsync(challenge.Id, ct);
        var submissionIds = submissions.Select(s => s.Id).ToList();

        var likeCounts = await _likes.GetCountsForTargetsAsync(submissionIds, TargetType.WeeklyChallengeSubmission);
        var likedByMe = query.CurrentUserId.HasValue
            ? await _likes.GetLikedTargetIdsAsync(query.CurrentUserId.Value, submissionIds, TargetType.WeeklyChallengeSubmission)
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
        return new PagedResult<WeeklyChallengeSubmissionDto>(items, totalCount, page, pageSize, totalPages);
    }
}
