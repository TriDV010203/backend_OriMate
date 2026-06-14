using OrigamiPlatform.Application.DTOs.FamilyProjects;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.FamilyProjects;

public class GetFamilyProjectHandler
{
    private readonly IFamilyProjectRepository _projects;

    public GetFamilyProjectHandler(IFamilyProjectRepository projects)
        => _projects = projects;

    public async Task<FamilyProjectDto> HandleAsync(
        GetFamilyProjectQuery query,
        CancellationToken ct = default)
    {
        var project = await _projects.GetByIdWithMembersAsync(query.ProjectId, ct)
            ?? throw new NotFoundException("Family project not found.");

        // Projects are private — only active members may view their content.
        var isMember = project.Members.Any(
            m => m.UserId == query.CurrentUserId && m.Status == MemberStatus.Active);
        if (!isMember)
            throw new ForbiddenException("You are not a member of this family project.");

        return project.ToDto();
    }
}
