using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.WeeklyChallenge;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Constants;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.WeeklyChallenge;

// Honor-system photo submission, mirror SubmitDailyChallengeHandler. Streak dùng chung
// ChallengeStreakService — nộp bài Thử thách tuần vào Chủ Nhật cũng nối tiếp cùng chuỗi với
// Thử thách ngày.
public class SubmitWeeklyChallengeHandler
{
    private const int MaxPhotoUrlLength = 512;
    private const int MaxNoteLength = 500;

    private readonly IWeeklyChallengeRepository _challenges;
    private readonly IWeeklyChallengeSubmissionRepository _submissions;
    private readonly ChallengeStreakService _challengeStreak;
    private readonly IBlockedWordService _blockedWordService;
    private readonly HatGapAwardService _hatGap;

    public SubmitWeeklyChallengeHandler(
        IWeeklyChallengeRepository challenges,
        IWeeklyChallengeSubmissionRepository submissions,
        ChallengeStreakService challengeStreak,
        IBlockedWordService blockedWordService,
        HatGapAwardService hatGap)
        => (_challenges, _submissions, _challengeStreak, _blockedWordService, _hatGap)
            = (challenges, submissions, challengeStreak, blockedWordService, hatGap);

    public async Task<WeeklyChallengeSubmissionDto> HandleAsync(
        SubmitWeeklyChallengeCommand command, CancellationToken ct = default)
    {
        Validate(command.Request.PhotoUrl, command.Request.Note);

        if (!string.IsNullOrWhiteSpace(command.Request.Note)
            && await _blockedWordService.ContainsBlockedWordAsync(command.Request.Note, ct))
            throw new DomainException("Nội dung chứa từ ngữ không phù hợp.");

        var today = GetTodayGmt7();
        var challenge = await _challenges.GetByDateAsync(today, ct)
            ?? throw new NotFoundException("Hiện chưa có Thử thách tuần — chỉ mở vào Chủ Nhật.");

        if (challenge.Status != DailyChallengeStatus.Active)
            throw new DomainException("Thử thách tuần chưa mở hoặc đã đóng.");

        if (await _submissions.ExistsAsync(challenge.Id, command.UserId, ct))
            throw new DomainException("Bạn đã nộp bài cho thử thách tuần này rồi.");

        var submission = new WeeklyChallengeSubmission
        {
            Id = Guid.NewGuid(),
            WeeklyChallengeId = challenge.Id,
            UserId = command.UserId,
            PhotoUrl = command.Request.PhotoUrl,
            Note = command.Request.Note,
            CreatedAt = DateTime.UtcNow
        };

        await _submissions.AddAsync(submission, ct);

        await _challengeStreak.UpdateAsync(command.UserId, ct);
        await AwardParticipationAsync(command.UserId, ct);

        return new WeeklyChallengeSubmissionDto(
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
                userId, HatGapEconomy.WeeklyChallengeParticipateReward, HatGapTransactionType.Earn,
                "WeeklyChallengeParticipate", ct);
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
