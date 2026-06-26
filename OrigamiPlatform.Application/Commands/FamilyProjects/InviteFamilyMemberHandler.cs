using OrigamiPlatform.Application.DTOs.FamilyProjects;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.FamilyProjects;

public class InviteFamilyMemberHandler
{
    private const int MaxMembers = 6;          // BV-26: owner + 5
    private const int InvitationValidHours = 48; // BV-28

    private readonly IFamilyProjectRepository _projects;
    private readonly IUserRepository _users;
    private readonly INotificationService _notifications;

    public InviteFamilyMemberHandler(
        IFamilyProjectRepository projects,
        IUserRepository users,
        INotificationService notifications)
        => (_projects, _users, _notifications) = (projects, users, notifications);

    public async Task<FamilyProjectDto> HandleAsync(
        InviteFamilyMemberCommand command,
        CancellationToken ct = default)
    {
        var email = (command.Email ?? string.Empty).Trim();
        if (email.Length == 0)
            throw new DomainException("An email address is required to invite a member.");

        var project = await _projects.GetByIdWithMembersAsync(command.ProjectId, ct)
            ?? throw new NotFoundException("Family project not found.");

        if (project.OwnerId != command.RequesterId)
            throw new ForbiddenException("Only the project owner can invite members.");

        if (project.Status != ProjectStatus.Active)
            throw new DomainException("Members can only be invited to an active project.");

        // Invitations are by registered email only.
        var invitee = await _users.GetByEmailAsync(email, ct)
            ?? throw new DomainException("No registered user was found with this email.");

        if (invitee.Id == project.OwnerId)
            throw new DomainException("The owner is already part of the project.");

        var now = DateTime.UtcNow;
        var expiresAt = now.AddHours(InvitationValidHours);

        // BV-26 / NAC-02: at most 6 active + pending members (excluding the invitee themselves).
        var othersCount = project.Members.Count(m => m.UserId != invitee.Id && IsCounted(m, now));
        if (othersCount >= MaxMembers)
            throw new DomainException($"A family project can have at most {MaxMembers} members.");

        var existing = project.Members.FirstOrDefault(m => m.UserId == invitee.Id);
        if (existing is not null)
        {
            if (existing.Status == MemberStatus.Active)
                throw new DomainException("This user is already a member of the project.");

            if (existing.Status == MemberStatus.Invited && existing.ExpiresAt > now)
                throw new DomainException("This user already has a pending invitation.");

            // Declined or expired invitation may be resent by the owner.
            existing.Status = MemberStatus.Invited;
            existing.InvitedAt = now;
            existing.ExpiresAt = expiresAt;
            existing.JoinedAt = null;
            await _projects.UpdateMemberAsync(existing, ct);
        }
        else
        {
            var member = new FamilyProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = invitee.Id,
                Status = MemberStatus.Invited,
                InvitedAt = now,
                ExpiresAt = expiresAt
            };
            await _projects.AddMemberAsync(member, ct);
        }

        await _notifications.NotifyUserAsync(
            invitee.Id,
            NotificationType.FamilyInvite,
            $"You were invited to the family project \"{project.Name}\".",
            "FamilyProject",
            project.Id,
            ct);

        var updated = await _projects.GetByIdWithMembersAsync(project.Id, ct)
            ?? throw new NotFoundException("Family project not found after inviting.");

        return updated.ToDto();
    }

    private static bool IsCounted(FamilyProjectMember member, DateTime now)
        => member.Status == MemberStatus.Active
            || (member.Status == MemberStatus.Invited && member.ExpiresAt > now);
}
