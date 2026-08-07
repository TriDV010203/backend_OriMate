using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.DailyChallenge;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Constants;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.DailyChallenge;

// FT-34: honor-system photo submission — deliberately NOT gated on TutorialStepProgress.
// A user who forgot how to fold can jump to the tutorial and come back to submit later.
public class SubmitDailyChallengeHandler
{
    private const int MaxPhotoUrlLength = 512;
    private const int MaxNoteLength = 500;

    private readonly IDailyChallengeRepository _challenges;
    private readonly IDailyChallengeSubmissionRepository _submissions;
    private readonly ChallengeStreakService _challengeStreak;
    private readonly IBlockedWordService _blockedWordService;
    private readonly HatGapAwardService _hatGap;

    public SubmitDailyChallengeHandler(
        IDailyChallengeRepository challenges,
        IDailyChallengeSubmissionRepository submissions,
        ChallengeStreakService challengeStreak,
        IBlockedWordService blockedWordService,
        HatGapAwardService hatGap)
        => (_challenges, _submissions, _challengeStreak, _blockedWordService, _hatGap)
            = (challenges, submissions, challengeStreak, blockedWordService, hatGap);

    public async Task<DailyChallengeSubmissionDto> HandleAsync(
        SubmitDailyChallengeCommand command, CancellationToken ct = default)
    {
        Validate(command.Request.PhotoUrl, command.Request.Note);

        if (!string.IsNullOrWhiteSpace(command.Request.Note)
            && await _blockedWordService.ContainsBlockedWordAsync(command.Request.Note, ct))
            throw new DomainException("Nội dung chứa từ ngữ không phù hợp.");

        var today = GetTodayGmt7();
        var challenge = await _challenges.GetByDateAsync(today, ct)
            ?? throw new NotFoundException("Hôm nay chưa có Thử thách ngày.");

        if (challenge.Status != DailyChallengeStatus.Active)
            throw new DomainException("Thử thách hôm nay chưa mở hoặc đã đóng.");

        if (await _submissions.ExistsAsync(challenge.Id, command.UserId, ct))
            throw new DomainException("Bạn đã nộp bài cho thử thách hôm nay rồi.");

        var submission = new DailyChallengeSubmission
        {
            Id = Guid.NewGuid(),
            DailyChallengeId = challenge.Id,
            UserId = command.UserId,
            PhotoUrl = command.Request.PhotoUrl,
            Note = command.Request.Note,
            CreatedAt = DateTime.UtcNow
        };

        await _submissions.AddAsync(submission, ct);

        await _challengeStreak.UpdateAsync(command.UserId, ct);
        await AwardParticipationAsync(command.UserId, ct);

        return new DailyChallengeSubmissionDto(
            submission.Id, command.UserId, null, null,
            submission.PhotoUrl, submission.Note,
            LikeCount: 0, IsLikedByCurrentUser: false, FinalRank: null,
            submission.CreatedAt);
    }

    private async Task AwardParticipationAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            await _hatGap.AwardAsync(
                userId, HatGapEconomy.DailyChallengeParticipateReward, HatGapTransactionType.Earn,
                "DailyChallengeParticipate", ct);
        }
        catch
        {
            // Hạt Gấp award failure must not affect the main submission flow
        }
    }

    private static void Validate(string photoUrl, string? note)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
            throw new DomainException("Ảnh nộp bài là bắt buộc.");

        if (photoUrl.Length > MaxPhotoUrlLength)
            throw new DomainException($"Photo URL must not exceed {MaxPhotoUrlLength} characters.");

        if (note is { Length: > MaxNoteLength })
            throw new DomainException($"Note must not exceed {MaxNoteLength} characters.");
    }

    private static DateOnly GetTodayGmt7() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
}
