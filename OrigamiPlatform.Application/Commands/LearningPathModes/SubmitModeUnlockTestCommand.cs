using OrigamiPlatform.Application.DTOs.LearningPathModes;

namespace OrigamiPlatform.Application.Commands.LearningPathModes;

public record SubmitModeUnlockTestCommand(Guid UserId, Guid ModeId, SubmitModeUnlockTestRequest Request);
