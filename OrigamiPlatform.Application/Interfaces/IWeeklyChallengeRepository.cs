using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Interfaces;

public interface IWeeklyChallengeRepository
{
    Task<WeeklyChallenge?> GetByDateAsync(DateOnly challengeDate, CancellationToken ct = default);
    Task<WeeklyChallenge?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(WeeklyChallenge challenge, CancellationToken ct = default);
    Task UpdateAsync(WeeklyChallenge challenge, CancellationToken ct = default);

    /// <summary>Tutorial ids already used as a weekly challenge since <paramref name="sinceDate"/> — used to avoid repeats when auto-picking.</summary>
    Task<HashSet<Guid>> GetRecentlyUsedTutorialIdsAsync(DateOnly sinceDate, CancellationToken ct = default);

    /// <summary>Published, non-deleted candidate tutorials (optionally filtered by difficulty, excluding given ids), with their Achievement count as a popularity proxy for weighted auto-pick.</summary>
    Task<List<(Tutorial Tutorial, int AchievementCount)>> GetEligibleCandidatesAsync(
        IReadOnlySet<Guid> excludeTutorialIds,
        TutorialDifficulty? difficulty,
        CancellationToken ct = default);

    Task<PagedResult<WeeklyChallenge>> GetAllForAdminAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        DailyChallengeStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>How many times a tutorial's author has had one of their tutorials picked as the weekly challenge — used for the "Author" badges.</summary>
    Task<int> CountByTutorialAuthorAsync(Guid authorUserId, CancellationToken ct = default);
}
