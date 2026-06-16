using OrigamiPlatform.Application.DTOs.FamilyProjects;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.FamilyProjects;

public class CompleteStepHandler
{
    private readonly IFamilyProjectRepository _projects;

    public CompleteStepHandler(IFamilyProjectRepository projects)
        => _projects = projects;

    public async Task<StepProgressDto> HandleAsync(CompleteStepCommand command, CancellationToken ct = default)
    {
        var project = await _projects.GetByIdWithMembersAsync(command.ProjectId, ct)
            ?? throw new NotFoundException("Family project not found.");

        // NAC-01: only active members may interact with a private project.
        var isMember = project.Members.Any(
            m => m.UserId == command.UserId && m.Status == MemberStatus.Active);
        if (!isMember)
            throw new ForbiddenException("You are not a member of this family project.");

        if (project.Status != ProjectStatus.Active)
            throw new DomainException("Progress can only be updated on an active project.");

        // The step must belong to this project's tutorial.
        if (!await _projects.StepBelongsToTutorialAsync(command.StepId, project.TutorialId, ct))
            throw new NotFoundException("Step not found in this project's tutorial.");

        // BV-29: each member can tick each step only once.
        if (await _projects.StepProgressExistsAsync(command.ProjectId, command.StepId, command.UserId, ct))
            throw new DomainException("You have already completed this step.");

        var now = DateTime.UtcNow;
        var progress = new FamilyProjectStepProgress
        {
            Id = Guid.NewGuid(),
            ProjectId = command.ProjectId,
            StepId = command.StepId,
            CompletedBy = command.UserId,
            CompletedAt = now
        };

        await _projects.AddStepProgressAsync(progress, ct);

        return new StepProgressDto(command.StepId, command.UserId, now);
    }
}
