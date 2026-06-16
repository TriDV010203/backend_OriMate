using OrigamiPlatform.Application.DTOs.FamilyProjects;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.FamilyProjects;

public class GetProjectProgressHandler
{
    private readonly IFamilyProjectRepository _projects;

    public GetProjectProgressHandler(IFamilyProjectRepository projects)
        => _projects = projects;

    public async Task<ProjectProgressDto> HandleAsync(
        GetProjectProgressQuery query,
        CancellationToken ct = default)
    {
        var project = await _projects.GetByIdWithMembersAsync(query.ProjectId, ct)
            ?? throw new NotFoundException("Family project not found.");

        // NAC-01: only active members may view a private project's progress.
        var isMember = project.Members.Any(
            m => m.UserId == query.CurrentUserId && m.Status == MemberStatus.Active);
        if (!isMember)
            throw new ForbiddenException("You are not a member of this family project.");

        var steps = await _projects.GetTutorialStepsAsync(project.TutorialId, ct);
        var progresses = await _projects.GetStepProgressesAsync(query.ProjectId, ct);

        var completionsByStep = progresses
            .GroupBy(p => p.StepId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Guid>)g.Select(p => p.CompletedBy).ToList());

        var stepDtos = steps
            .Select(s => new StepStatusDto(
                s.Id,
                s.StepOrder,
                completionsByStep.ContainsKey(s.Id),
                completionsByStep.TryGetValue(s.Id, out var users) ? users : Array.Empty<Guid>()))
            .ToList();

        var totalSteps = steps.Count;
        var completedSteps = stepDtos.Count(s => s.Completed);

        var memberDtos = project.Members
            .Where(m => m.Status == MemberStatus.Active)
            .Select(m => new MemberProgressDto(
                m.UserId,
                m.User?.Email,
                progresses.Count(p => p.CompletedBy == m.UserId)))
            .ToList();

        return new ProjectProgressDto(
            project.Id,
            totalSteps,
            completedSteps,
            totalSteps > 0 && completedSteps == totalSteps,
            stepDtos,
            memberDtos);
    }
}
