using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Commands.TutorialProgress;

public record CompleteTutorialCommand(
    Guid UserId,
    Guid TutorialId,
    PerceivedDifficulty? PerceivedDifficulty,
    string? PhotoUrl,
    string? Note,
    bool IsPublic = true
);
