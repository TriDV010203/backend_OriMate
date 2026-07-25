using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.LearningPaths;

/// <summary>Archive is the soft-delete equivalent for a learning path (no hard delete) —
/// hides it from public browsing while keeping the record and its items intact.</summary>
public class ArchiveLearningPathHandler
{
    private readonly ILearningPathRepository _learningPathRepo;

    public ArchiveLearningPathHandler(ILearningPathRepository learningPathRepo) => _learningPathRepo = learningPathRepo;

    public async Task HandleAsync(ArchiveLearningPathCommand command, CancellationToken ct = default)
    {
        var learningPath = await _learningPathRepo.GetByIdForAdminAsync(command.LearningPathId, ct)
            ?? throw new NotFoundException($"Learning path {command.LearningPathId} not found.");

        if (learningPath.Status == LearningPathStatus.Archived)
            throw new DomainException("This learning path is already archived.");

        learningPath.Status = LearningPathStatus.Archived;
        learningPath.UpdatedAt = DateTime.UtcNow;

        await _learningPathRepo.UpdateAsync(learningPath, ct);
    }
}
