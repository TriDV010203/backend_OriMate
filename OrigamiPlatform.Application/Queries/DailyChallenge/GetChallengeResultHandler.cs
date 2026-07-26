using OrigamiPlatform.Application.DTOs.DailyChallenge;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.DailyChallenge;

public class GetChallengeResultHandler
{
    private readonly IDailyChallengeRepository _challenges;
    private readonly IDailyChallengeSubmissionRepository _submissions;
    private readonly ILikeRepository _likes;

    public GetChallengeResultHandler(
        IDailyChallengeRepository challenges,
        IDailyChallengeSubmissionRepository submissions,
        ILikeRepository likes)
        => (_challenges, _submissions, _likes) = (challenges, submissions, likes);

    public async Task<ChallengeResultDto> HandleAsync(GetChallengeResultQuery query, CancellationToken ct = default)
    {
        var challenge = await _challenges.GetByDateAsync(query.ChallengeDate, ct)
            ?? throw new NotFoundException("Không tìm thấy Thử thách ngày cho ngày này.");

        var submissions = await _submissions.GetByChallengeAsync(challenge.Id, ct);
        var submissionIds = submissions.Select(s => s.Id).ToList();
        var likeCounts = await _likes.GetCountsForTargetsAsync(submissionIds, TargetType.DailyChallengeSubmission);

        var top = submissions
            .Where(s => s.FinalRank is > 0 and <= 3)
            .OrderBy(s => s.FinalRank)
            .Select(s => s.ToDto(likeCounts.GetValueOrDefault(s.Id, 0), false))
            .ToList();

        return new ChallengeResultDto(
            challenge.ChallengeDate,
            challenge.Status.ToString(),
            challenge.TutorialId,
            challenge.Tutorial.Title,
            submissions.Count,
            top);
    }
}
