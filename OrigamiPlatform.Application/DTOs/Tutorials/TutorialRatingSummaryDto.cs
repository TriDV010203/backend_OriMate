using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.Tutorials;

public record TutorialRatingSummaryDto(
    IReadOnlyDictionary<PerceivedDifficulty, int> Counts,
    int TotalCount
);
