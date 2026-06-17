using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.FamilyProjects;
using OrigamiPlatform.Application.DTOs.FamilyProjects;
using OrigamiPlatform.Application.Queries.FamilyProjects;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/family-projects")]
[Authorize]
public class FamilyProjectsController : ControllerBase
{
    private readonly CreateFamilyProjectHandler _createProject;
    private readonly GetFamilyProjectHandler _getProject;
    private readonly InviteFamilyMemberHandler _inviteMember;
    private readonly RespondFamilyInvitationHandler _respondInvitation;
    private readonly CompleteStepHandler _completeStep;
    private readonly GetProjectProgressHandler _getProgress;
    private readonly MarkProjectCompletedHandler _markCompleted;
    private readonly ShareProjectHandler _shareProject;

    public FamilyProjectsController(
        CreateFamilyProjectHandler createProject,
        GetFamilyProjectHandler getProject,
        InviteFamilyMemberHandler inviteMember,
        RespondFamilyInvitationHandler respondInvitation,
        CompleteStepHandler completeStep,
        GetProjectProgressHandler getProgress,
        MarkProjectCompletedHandler markCompleted,
        ShareProjectHandler shareProject)
        => (_createProject, _getProject, _inviteMember, _respondInvitation, _completeStep, _getProgress, _markCompleted, _shareProject)
            = (createProject, getProject, inviteMember, respondInvitation, completeStep, getProgress, markCompleted, shareProject);

    [HttpPost]
    public async Task<IActionResult> Create(CreateFamilyProjectRequest request, CancellationToken ct)
    {
        var result = await _createProject.HandleAsync(
            new CreateFamilyProjectCommand(GetCurrentUserId(), request), ct);

        return CreatedAtAction(nameof(GetById), new { projectId = result.Id }, result);
    }

    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> GetById(Guid projectId, CancellationToken ct)
    {
        var result = await _getProject.HandleAsync(
            new GetFamilyProjectQuery(projectId, GetCurrentUserId()), ct);

        return Ok(result);
    }

    [HttpPost("{projectId:guid}/invitations")]
    public async Task<IActionResult> Invite(
        Guid projectId,
        InviteFamilyMemberRequest request,
        CancellationToken ct)
    {
        var result = await _inviteMember.HandleAsync(
            new InviteFamilyMemberCommand(projectId, GetCurrentUserId(), request.Email), ct);

        return Ok(result);
    }

    [HttpPost("{projectId:guid}/invitation/accept")]
    public async Task<IActionResult> AcceptInvitation(Guid projectId, CancellationToken ct)
    {
        var result = await _respondInvitation.HandleAsync(
            new RespondFamilyInvitationCommand(projectId, GetCurrentUserId(), Accept: true), ct);

        return Ok(result);
    }

    [HttpPost("{projectId:guid}/invitation/decline")]
    public async Task<IActionResult> DeclineInvitation(Guid projectId, CancellationToken ct)
    {
        var result = await _respondInvitation.HandleAsync(
            new RespondFamilyInvitationCommand(projectId, GetCurrentUserId(), Accept: false), ct);

        return Ok(result);
    }

    [HttpPost("{projectId:guid}/steps/{stepId:guid}/complete")]
    public async Task<IActionResult> CompleteStep(Guid projectId, Guid stepId, CancellationToken ct)
    {
        var result = await _completeStep.HandleAsync(
            new CompleteStepCommand(projectId, stepId, GetCurrentUserId()), ct);

        return Ok(result);
    }

    [HttpGet("{projectId:guid}/progress")]
    public async Task<IActionResult> GetProgress(Guid projectId, CancellationToken ct)
    {
        var result = await _getProgress.HandleAsync(
            new GetProjectProgressQuery(projectId, GetCurrentUserId()), ct);

        return Ok(result);
    }

    [HttpPost("{projectId:guid}/complete")]
    public async Task<IActionResult> CompleteProject(Guid projectId, CancellationToken ct)
    {
        var result = await _markCompleted.HandleAsync(
            new MarkProjectCompletedCommand(projectId, GetCurrentUserId()), ct);

        return Ok(result);
    }

    [HttpPost("{projectId:guid}/share")]
    public async Task<IActionResult> Share(
        Guid projectId,
        [FromBody] ShareProjectRequest? request,
        CancellationToken ct)
    {
        var result = await _shareProject.HandleAsync(
            new ShareProjectCommand(projectId, GetCurrentUserId(), request?.Note), ct);

        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
            throw new ForbiddenException("Invalid user token.");

        return userId;
    }
}
