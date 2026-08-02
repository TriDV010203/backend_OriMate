namespace OrigamiPlatform.Application.DTOs.Gamification;

// TotalEarned drives Level (lifetime earned, unaffected by spending); Balance is the current spendable amount.
public record HatGapLevelDto(
    int Level,
    int TotalEarned,
    int Balance,
    int CurrentLevelFloor,
    int NextLevelThreshold,
    int HatGapToNextLevel,
    decimal ProgressPercent);
