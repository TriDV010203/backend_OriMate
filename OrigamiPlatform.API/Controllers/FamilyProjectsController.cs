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

    public FamilyProjectsController(
        CreateFamilyProjectHandler createProject,
        GetFamilyProjectHandler getProject)
        => (_createProject, _getProject) = (createProject, getProject);

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

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
            throw new ForbiddenException("Invalid user token.");

        return userId;
    }
}
