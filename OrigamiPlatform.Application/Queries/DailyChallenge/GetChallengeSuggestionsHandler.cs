using OrigamiPlatform.Application.DTOs.DailyChallenge;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.DailyChallenge;

// FT-34: read-only preview for the admin "schedule" screen — does not write anything.
// Ranked by Achievement count (completions) as a simple popularity/quality proxy.
public class GetChallengeSuggestionsHandler
{
    private readonly IDailyChallengeRepository _challenges;

    public GetChallengeSuggestionsHandler(IDailyChallengeRepository challenges) => _challenges = challenges;

    public async Task<List<ChallengeSuggestionDto>> HandleAsync(
        GetChallengeSuggestionsQuery query, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var excludeIds = await _challenges.GetRecentlyUsedTutorialIdsAsync(today.AddDays(-30), ct);

        var candidates = await _challenges.GetEligibleCandidatesAsync(excludeIds, difficulty: null, ct);
        if (candidates.Count == 0)
            candidates = await _challenges.GetEligibleCandidatesAsync(new HashSet<Guid>(), difficulty: null, ct);

        var count = Math.Clamp(query.Count, 1, 20);

        return candidates
            .OrderByDescending(c => c.AchievementCount)
            .Take(count)
            .Select(c => new ChallengeSuggestionDto(
                c.Tutorial.Id, c.Tutorial.Title, c.Tutorial.Slug, c.Tutorial.CoverImageUrl,
                c.Tutorial.Difficulty.ToString(), c.Tutorial.CategoryId, c.AchievementCount))
            .ToList();
    }
}
