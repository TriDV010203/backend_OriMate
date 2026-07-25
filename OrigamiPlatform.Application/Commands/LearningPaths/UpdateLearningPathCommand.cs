using OrigamiPlatform.Application.DTOs.LearningPaths;

namespace OrigamiPlatform.Application.Commands.LearningPaths;

public record UpdateLearningPathCommand(Guid LearningPathId, Guid ActorId, UpdateLearningPathRequest Request);
