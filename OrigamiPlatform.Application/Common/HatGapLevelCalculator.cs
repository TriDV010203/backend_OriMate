using OrigamiPlatform.Domain.Constants;

namespace OrigamiPlatform.Application.Common;

// Converts lifetime-earned Hạt Gấp into a Level, using the cumulative curve
// Level N requires N × (N + 1) / 2 × HatGapEconomy.LevelHatGapStep total Hạt ever earned
// (spending Hạt does not reduce level — it is based on total earned, not current balance).
public static class HatGapLevelCalculator
{
    public static int CumulativeHatGapForLevel(int level)
        => level * (level + 1) / 2 * HatGapEconomy.LevelHatGapStep;

    public static int GetLevel(int totalEarned)
    {
        var level = 0;
        while (CumulativeHatGapForLevel(level + 1) <= totalEarned)
            level++;

        return level;
    }

    public static HatGapLevelProgress GetProgress(int totalEarned)
    {
        var level = GetLevel(totalEarned);
        var currentLevelFloor = CumulativeHatGapForLevel(level);
        var nextLevelThreshold = CumulativeHatGapForLevel(level + 1);
        var hatGapIntoLevel = totalEarned - currentLevelFloor;
        var hatGapForLevel = nextLevelThreshold - currentLevelFloor;
        var progressPercent = hatGapForLevel == 0 ? 0 : Math.Round(hatGapIntoLevel * 100m / hatGapForLevel, 1);

        return new HatGapLevelProgress(
            Level: level,
            TotalEarned: totalEarned,
            CurrentLevelFloor: currentLevelFloor,
            NextLevelThreshold: nextLevelThreshold,
            HatGapToNextLevel: nextLevelThreshold - totalEarned,
            ProgressPercent: progressPercent);
    }
}

public record HatGapLevelProgress(
    int Level,
    int TotalEarned,
    int CurrentLevelFloor,
    int NextLevelThreshold,
    int HatGapToNextLevel,
    decimal ProgressPercent);
