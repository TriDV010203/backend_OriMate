using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.LearningPaths;

public class PublishLearningPathHandler
{
    private readonly ILearningPathRepository _learningPathRepo;

    public PublishLearningPathHandler(ILearningPathRepository learningPathRepo) => _learningPathRepo = learningPathRepo;

    public async Task HandleAsync(PublishLearningPathCommand command, CancellationToken ct = default)
    {
        var learningPath = await _learningPathRepo.GetByIdForAdminAsync(command.LearningPathId, ct)
            ?? throw new NotFoundException($"Learning path {command.LearningPathId} not found.");

        if (learningPath.Status == LearningPathStatus.Published)
            throw new DomainException("This learning path is already published.");

        if (learningPath.Items.Count == 0)
            throw new DomainException("Add at least one tutorial before publishing a learning path.");

        var now = DateTime.UtcNow;
        learningPath.Status = LearningPathStatus.Published;
        learningPath.PublishedAt ??= now;
        learningPath.UpdatedAt = now;

        await _learningPathRepo.UpdateAsync(learningPath, ct);
    }
}
