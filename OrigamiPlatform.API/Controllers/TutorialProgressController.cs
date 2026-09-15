using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.TutorialProgress;
using OrigamiPlatform.Application.DTOs.TutorialProgress;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/tutorials")]
[Authorize]
public class TutorialProgressController : ControllerBase
{
    private readonly CompleteTutorialHandler _completeTutorial;
    private readonly RaiseStuckFlagHandler _raiseStuck;

    public TutorialProgressController(
        CompleteTutorialHandler completeTutorial,
        RaiseStuckFlagHandler raiseStuck)
        => (_completeTutorial, _raiseStuck) = (completeTutorial, raiseStuck);

    [HttpPost("{tutorialId:guid}/complete")]
    public async Task<IActionResult> CompleteTutorial(
        Guid tutorialId, CompleteTutorialRequest request, CancellationToken ct)
    {
        var result = await _completeTutorial.HandleAsync(
            new CompleteTutorialCommand(
                GetCurrentUserId(), tutorialId, request.PerceivedDifficulty, request.PhotoUrl, request.Note, request.IsPublic),
            ct);
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
