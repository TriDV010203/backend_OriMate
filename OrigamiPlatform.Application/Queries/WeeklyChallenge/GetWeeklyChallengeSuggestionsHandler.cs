using OrigamiPlatform.Application.DTOs.WeeklyChallenge;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.WeeklyChallenge;

// Read-only preview for the admin "schedule" screen — does not write anything. Lọc độ khó
// Advanced (Thử thách tuần luôn là Khó); nếu không đủ ứng viên Advanced thì nới lỏng để trang
// admin luôn có gợi ý thay vì trống trơn. Ranked by Achievement count (completions).
public class GetWeeklyChallengeSuggestionsHandler
{
    private readonly IWeeklyChallengeRepository _challenges;

    public GetWeeklyChallengeSuggestionsHandler(IWeeklyChallengeRepository challenges) => _challenges = challenges;

    public async Task<List<WeeklyChallengeSuggestionDto>> HandleAsync(
        GetWeeklyChallengeSuggestionsQuery query, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var excludeIds = await _challenges.GetRecentlyUsedTutorialIdsAsync(today.AddDays(-30), ct);

        var candidates = await _challenges.GetEligibleCandidatesAsync(excludeIds, TutorialDifficulty.Advanced, ct);
        if (candidates.Count == 0)
            candidates = await _challenges.GetEligibleCandidatesAsync(excludeIds, difficulty: null, ct);
        if (candidates.Count == 0)
            candidates = await _challenges.GetEligibleCandidatesAsync(new HashSet<Guid>(), difficulty: null, ct);

        var count = Math.Clamp(query.Count, 1, 20);

        return candidates
            .OrderByDescending(c => c.AchievementCount)
            .Take(count)
            .Select(c => new WeeklyChallengeSuggestionDto(
                c.Tutorial.Id, c.Tutorial.Title, c.Tutorial.Slug, c.Tutorial.CoverImageUrl,
                c.Tutorial.Difficulty.ToString(), c.Tutorial.CategoryId, c.AchievementCount))
            .ToList();
    }
}
