using OrigamiPlatform.Application.DTOs.FamilyProjects;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.FamilyProjects;

public class CreateFamilyProjectHandler
{
    private const int MaxActiveProjects = 5;   // BV-27
    private const int MaxNameLength = 200;

    private readonly IFamilySubscriptionRepository _subscriptions;
    private readonly IFamilyProjectRepository _projects;

    public CreateFamilyProjectHandler(
        IFamilySubscriptionRepository subscriptions,
        IFamilyProjectRepository projects)
        => (_subscriptions, _projects) = (subscriptions, projects);

    public async Task<FamilyProjectDto> HandleAsync(
        CreateFamilyProjectCommand command,
        CancellationToken ct = default)
    {
        var name = (command.Request.Name ?? string.Empty).Trim();
        if (name.Length is 0 or > MaxNameLength)
            throw new DomainException($"Project name must be between 1 and {MaxNameLength} characters.");

        // NAC-01: the owner must hold an active family plan.
        var subscription = await _subscriptions.GetActiveByUserAsync(command.OwnerId, ct)
            ?? throw new DomainException("An active family plan is required to create a family project.");

        // NAC-03 / BR-28: only free, published tutorials are eligible.
        if (!await _projects.IsFreePublishedTutorialAsync(command.Request.TutorialId, ct))
            throw new DomainException("Only free, published tutorials can be used for a family project.");

        // BV-27: at most 5 active projects per subscription.
        var activeCount = await _projects.CountActiveBySubscriptionAsync(subscription.Id, ct);
        if (activeCount >= MaxActiveProjects)
            throw new DomainException($"You can have at most {MaxActiveProjects} active family projects.");

        var now = DateTime.UtcNow;
        var projectId = Guid.NewGuid();
        var project = new FamilyProject
        {
            Id = projectId,
            OwnerId = command.OwnerId,
            TutorialId = command.Request.TutorialId,
            SubscriptionId = subscription.Id,
            Name = name,
            Status = ProjectStatus.Active,
            CreatedAt = now,
            // The owner is the first member (counts toward the 6-member cap, BV-26).
            Members =
            {
                new FamilyProjectMember
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    UserId = command.OwnerId,
                    Status = MemberStatus.Active,
                    InvitedAt = now,
                    JoinedAt = now
                }
            }
        };

        await _projects.AddAsync(project, ct);

        var created = await _projects.GetByIdWithMembersAsync(projectId, ct)
            ?? throw new NotFoundException("Family project not found after creation.");

        return created.ToDto();
    }
}
