namespace OrigamiPlatform.Application.Features.Tutorials.DTOs;

public record CreateTutorialRequest(
    string Title,
    string Description,
    int CategoryId,
    string Difficulty,
    string Type,
    string? CoverImageUrl,
    IList<CreateTutorialStepRequest>? Steps
);
