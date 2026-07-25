using OrigamiPlatform.Application.DTOs.LearningPaths;

namespace OrigamiPlatform.Application.Commands.LearningPaths;

public record CreateLearningPathCommand(Guid ActorId, CreateLearningPathRequest Request);
