using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.TutorialProgress;
using OrigamiPlatform.Application.Queries.TutorialProgress;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/tutorials")]
[Authorize]
public class TutorialProgressController : ControllerBase
{
    private readonly CompleteTutorialStepHandler _complete;
    private readonly UncompleteTutorialStepHandler _uncomplete;
    private readonly GetTutorialProgressHandler _getProgress;
    private readonly RaiseStuckFlagHandler _raiseStuck;

    public TutorialProgressController(
        CompleteTutorialStepHandler complete,
        UncompleteTutorialStepHandler uncomplete,
        GetTutorialProgressHandler getProgress,
        RaiseStuckFlagHandler raiseStuck)
        => (_complete, _uncomplete, _getProgress, _raiseStuck) = (complete, uncomplete, getProgress, raiseStuck);

    [HttpPost("{tutorialId:guid}/steps/{stepId:guid}/complete")]
    public async Task<IActionResult> CompleteStep(Guid tutorialId, Guid stepId, CancellationToken ct)
    {
        var result = await _complete.HandleAsync(
            new CompleteTutorialStepCommand(GetCurrentUserId(), tutorialId, stepId), ct);
        return Ok(result);
    }

    [HttpDelete("{tutorialId:guid}/steps/{stepId:guid}/complete")]
    public async Task<IActionResult> UncompleteStep(Guid tutorialId, Guid stepId, CancellationToken ct)
    {
        var result = await _uncomplete.HandleAsync(
            new UncompleteTutorialStepCommand(GetCurrentUserId(), tutorialId, stepId), ct);
        return Ok(result);
    }

    [HttpGet("{tutorialId:guid}/progress")]
    public async Task<IActionResult> GetProgress(Guid tutorialId, CancellationToken ct)
    {
        var result = await _getProgress.HandleAsync(
            new GetTutorialProgressQuery(GetCurrentUserId(), tutorialId), ct);
        return Ok(result);
    }

    [HttpPost("{tutorialId:guid}/steps/{stepId:guid}/stuck")]
    public async Task<IActionResult> RaiseStuck(Guid tutorialId, Guid stepId, CancellationToken ct)
    {
        var result = await _raiseStuck.HandleAsync(
            new RaiseStuckFlagCommand(GetCurrentUserId(), tutorialId, stepId), ct);
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
