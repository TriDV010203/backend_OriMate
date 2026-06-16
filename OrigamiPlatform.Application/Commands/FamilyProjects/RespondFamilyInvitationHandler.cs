using OrigamiPlatform.Application.DTOs.FamilyProjects;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.FamilyProjects;

public class RespondFamilyInvitationHandler
{
    private const int MaxMembers = 6; // BV-26

    private readonly IFamilyProjectRepository _projects;

    public RespondFamilyInvitationHandler(IFamilyProjectRepository projects)
        => _projects = projects;

    public async Task<FamilyProjectDto> HandleAsync(
        RespondFamilyInvitationCommand command,
        CancellationToken ct = default)
    {
        var project = await _projects.GetByIdWithMembersAsync(command.ProjectId, ct)
            ?? throw new NotFoundException("Family project not found.");

        var member = project.Members.FirstOrDefault(m => m.UserId == command.UserId);
        if (member is null || member.Status != MemberStatus.Invited)
            throw new DomainException("You have no pending invitation for this project.");

        var now = DateTime.UtcNow;

        // NAC-04 / BV-28: invitations expire after 48 hours.
        if (member.ExpiresAt.HasValue && member.ExpiresAt.Value < now)
            throw new DomainException("This invitation has expired.");

        if (command.Accept)
        {
            var activeCount = project.Members.Count(m => m.Status == MemberStatus.Active);
            if (activeCount >= MaxMembers)
                throw new DomainException("This family project is already full.");

            member.Status = MemberStatus.Active;
            member.JoinedAt = now;
        }
        else
        {
            member.Status = MemberStatus.Declined;
        }

        await _projects.UpdateMemberAsync(member, ct);

        var updated = await _projects.GetByIdWithMembersAsync(project.Id, ct)
            ?? throw new NotFoundException("Family project not found after responding.");

        return updated.ToDto();
    }
}
