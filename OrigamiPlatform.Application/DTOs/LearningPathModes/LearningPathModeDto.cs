namespace OrigamiPlatform.Application.DTOs.LearningPathModes;

/// <summary>Public roadmap tab: mode metadata + whether the current viewer has it unlocked.
/// Unlocking a non-entry mode requires only an approved unlock-test submission for that mode —
/// no dependency on having completed any Learning Path in an earlier mode.</summary>
public record LearningPathModeDto(
    Guid Id,
    string Name,
    string? Description,
    int SortOrder,
    bool IsEntryMode,
    bool IsUnlocked,
    LearningPathModeUnlockTestStatusDto? UnlockTest
);
