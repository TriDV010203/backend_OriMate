using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Constants;

// BR-SEEDS: central point values for the "Hạt Gấp" economy —
// tutorial completion, daily challenge participation, streak milestones,
// personal milestones and learning-path completion.
public static class HatGapEconomy
{
    // Hoàn thành 1 tutorial (first-time, all steps done) — fixed by difficulty, no streak/FFD multiplier.
    public static readonly IReadOnlyDictionary<TutorialDifficulty, int> TutorialCompletionReward = new Dictionary<TutorialDifficulty, int>
    {
        [TutorialDifficulty.Beginner] = 1,
        [TutorialDifficulty.Intermediate] = 2,
        [TutorialDifficulty.Advanced] = 3
    };

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

    // FT-34: nộp bài vào Thử thách ngày (mỗi lần nộp, không phụ thuộc kết quả).
    public const int DailyChallengeParticipateReward = 1;

    // FT-34: tutorial của tác giả được chọn làm Thử thách ngày.
    public const int DailyChallengeAuthorSelectedReward = 3;

    // FT-34: thưởng theo hạng khi Thử thách ngày đóng sổ (hạng 1/2/3).
    public static readonly IReadOnlyDictionary<int, int> ChallengeRankReward = new Dictionary<int, int>
    {
        [1] = 15,
        [2] = 10,
        [3] = 5
    };

    // Level curve: cumulative lifetime Hạt earned needed to reach level N grows by this step each level
    // (Level N total = N × (N + 1) / 2 × LevelHatGapStep — Level 1: 10, Level 2: 30, Level 3: 60, Level 4: 100, ...).
    public const int LevelHatGapStep = 10;
}
