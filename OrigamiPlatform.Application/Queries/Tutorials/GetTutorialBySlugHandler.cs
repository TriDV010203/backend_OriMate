using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.Tutorials;

public class GetTutorialBySlugHandler
{
    private readonly ITutorialRepository _tutorials;

    public GetTutorialBySlugHandler(ITutorialRepository tutorials) => _tutorials = tutorials;

    public async Task<TutorialDetailDto> HandleAsync(
        GetTutorialBySlugQuery query, CancellationToken ct = default)
    {
        var tutorial = await _tutorials.GetPublishedBySlugAsync(query.Slug, ct)
            ?? throw new NotFoundException($"Tutorial '{query.Slug}' not found.");

        var steps = tutorial.Steps
            .OrderBy(s => s.StepOrder)
            .Select(s => new TutorialStepDto(s.Id, s.StepOrder, s.Description, s.ImageUrl))
            .ToList();

        return new TutorialDetailDto(
            tutorial.Id,
            tutorial.Title,
            tutorial.Slug,
            tutorial.Description,
            tutorial.CoverImageUrl,
            tutorial.Type.ToString(),
            tutorial.Difficulty,
            tutorial.CategoryId,
            tutorial.Category.Name,
            new AuthorDto(
                tutorial.Author.Id,
                tutorial.Author.Profile?.DisplayName ?? tutorial.Author.Email,
                tutorial.Author.Profile?.AvatarUrl),
            steps,
            tutorial.PublishedAt!.Value
        );
    }
}
