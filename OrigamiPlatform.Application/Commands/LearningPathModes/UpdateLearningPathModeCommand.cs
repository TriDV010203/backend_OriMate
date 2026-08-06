using OrigamiPlatform.Application.DTOs.LearningPathModes;

namespace OrigamiPlatform.Application.Commands.LearningPathModes;

public record UpdateLearningPathModeCommand(Guid ModeId, UpdateLearningPathModeRequest Request);
