using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.DTOs.FamilyProjects;

public static class FamilyProjectMapping
{
    public static FamilyProjectDto ToDto(this FamilyProject project)
        => new(
            project.Id,
            project.OwnerId,
            project.TutorialId,
            project.Tutorial?.Title,
            project.Name,
            project.Status.ToString(),
            project.CreatedAt,
            project.CompletedAt,
            project.Members
                .OrderBy(m => m.InvitedAt)
                .Select(m => new FamilyProjectMemberDto(
                    m.UserId,
                    m.User?.Email,
                    m.Status.ToString(),
                    m.InvitedAt,
                    m.JoinedAt))
                .ToList());
}
