using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Queries.Tutorials;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/tutorials")]
public class TutorialsController : ControllerBase
{
    private readonly GetTutorialsHandler _getTutorials;
    private readonly GetTutorialBySlugHandler _getTutorialBySlug;

    public TutorialsController(
        GetTutorialsHandler getTutorials,
        GetTutorialBySlugHandler getTutorialBySlug)
        => (_getTutorials, _getTutorialBySlug) = (getTutorials, getTutorialBySlug);

    [HttpGet]
    public async Task<IActionResult> GetTutorials(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] string? difficulty,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var result = await _getTutorials.HandleAsync(
            new GetTutorialsQuery(search, categoryId, difficulty, type, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await _getTutorialBySlug.HandleAsync(
            new GetTutorialBySlugQuery(slug), ct);
        return Ok(result);
    }
}
