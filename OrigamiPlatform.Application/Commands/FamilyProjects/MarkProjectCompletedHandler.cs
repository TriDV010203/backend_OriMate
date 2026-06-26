using OrigamiPlatform.Application.DTOs.FamilyProjects;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.FamilyProjects;

public class MarkProjectCompletedHandler
{
    private readonly IFamilyProjectRepository _projects;

    public MarkProjectCompletedHandler(IFamilyProjectRepository projects)
        => _projects = projects;

    public async Task<FamilyProjectDto> HandleAsync(
        MarkProjectCompletedCommand command,
        CancellationToken ct = default)
    {
        var project = await _projects.GetByIdWithMembersAsync(command.ProjectId, ct)
            ?? throw new NotFoundException("Family project not found.");

        // Only the owner can complete the project (UC-40).
        if (project.OwnerId != command.UserId)
            throw new ForbiddenException("Only the project owner can mark the project as completed.");

        if (project.Status != ProjectStatus.Active)
            throw new DomainException("Only an active project can be marked as completed.");

        var steps = await _projects.GetTutorialStepsAsync(project.TutorialId, ct);
        if (steps.Count == 0)
            throw new DomainException("The project's tutorial has no steps to complete.");

        // BR-39 / NAC-02: every step must have at least one completion record.
        var progresses = await _projects.GetStepProgressesAsync(command.ProjectId, ct);
        var completedStepIds = progresses.Select(p => p.StepId).ToHashSet();
        if (!steps.All(s => completedStepIds.Contains(s.Id)))
            throw new DomainException(
                "All steps must have at least one completion before the project can be completed.");

        var now = DateTime.UtcNow;
        project.Status = ProjectStatus.Completed;
        project.CompletedAt = now;
        project.UpdatedAt = now;

        await _projects.UpdateAsync(project, ct);

        return project.ToDto();
    }
}
