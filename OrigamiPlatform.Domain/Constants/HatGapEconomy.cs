using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Constants;

// BR-SEEDS: central point values for the non-Clan "Hạt Gấp" economy —
// tutorial completion, daily quest (with streak multiplier + Free Fold Day cap),
// streak milestones, personal milestones and learning-path completion.
public static class HatGapEconomy
{
    // Hoàn thành 1 tutorial (first-time, all steps done) — fixed by difficulty, no streak/FFD multiplier.
    public static readonly IReadOnlyDictionary<TutorialDifficulty, int> TutorialCompletionReward = new Dictionary<TutorialDifficulty, int>
    {
        [TutorialDifficulty.Beginner] = 1,
        [TutorialDifficulty.Intermediate] = 2,
        [TutorialDifficulty.Advanced] = 3
    };

    // Hoàn thành Daily Quest — base reward before the streak multiplier below.
    public const int DailyQuestBaseReward = 1;

    // Streak multiplier tiers for the Daily Quest reward: (minimum CurrentStreak days, multiplier).
    // Ordered ascending; the highest tier whose threshold the streak meets or exceeds applies.
    public static readonly IReadOnlyList<(int MinStreakDays, decimal Multiplier)> StreakMultiplierTiers = new[]
    {
        (0, 1.0m),
        (7, 1.2m),
        (14, 1.5m),
        (30, 2.0m)
    };

    // Free Fold Day (Chủ nhật): the streak multiplier is capped at this value that day.
    public const decimal FreeFoldDayMultiplierCap = 1.5m;

    // Streak milestone bonus — awarded whenever CurrentStreak reaches one of these day counts.
    public static readonly IReadOnlyDictionary<int, int> StreakMilestoneReward = new Dictionary<int, int>
    {
        [7] = 5,
        [14] = 10,
        [30] = 20
    };

    // Personal Milestone bonus (by total completed tutorials) — every tier also grants a free Paper Pattern.
    public static readonly IReadOnlyDictionary<int, int> PersonalMilestoneReward = new Dictionary<int, int>
    {
        [10] = 15,
        [30] = 30,
        [50] = 60,
        [100] = 150
    };

    // Hoàn thành 1 lộ trình học (Learning Path) — awarded once per user per path.
    public const int LearningPathCompletionReward = 5;

    // Level curve: cumulative lifetime Hạt earned needed to reach level N grows by this step each level
    // (Level N total = N × (N + 1) / 2 × LevelHatGapStep — Level 1: 10, Level 2: 30, Level 3: 60, Level 4: 100, ...).
    public const int LevelHatGapStep = 10;

    public static decimal GetStreakMultiplier(int currentStreakDays, bool isFreeFoldDay)
    {
        var multiplier = 1.0m;
        foreach (var (minStreakDays, tierMultiplier) in StreakMultiplierTiers)
        {
            if (currentStreakDays >= minStreakDays)
                multiplier = tierMultiplier;
        }

        if (isFreeFoldDay && multiplier > FreeFoldDayMultiplierCap)
            multiplier = FreeFoldDayMultiplierCap;

        return multiplier;
    }
}
