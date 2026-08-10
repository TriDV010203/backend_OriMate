using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.TutorialProgress;

public record CompleteTutorialRequest(
    PerceivedDifficulty? PerceivedDifficulty,
    string? PhotoUrl,
    string? Note,
    bool IsPublic = true
);
