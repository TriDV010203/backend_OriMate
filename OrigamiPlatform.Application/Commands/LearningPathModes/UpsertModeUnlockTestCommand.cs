using OrigamiPlatform.Application.DTOs.LearningPathModes;

namespace OrigamiPlatform.Application.Commands.LearningPathModes;

public record UpsertModeUnlockTestCommand(Guid ModeId, UpsertModeUnlockTestRequest Request);
