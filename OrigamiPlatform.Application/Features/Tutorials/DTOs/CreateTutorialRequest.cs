namespace OrigamiPlatform.Application.Features.Tutorials.DTOs;

public record CreateTutorialRequest(
    string Title,
    string Description,
    int CategoryId,
    string Difficulty,
    string Type,
    string? CoverImageUrl,
    IList<CreateTutorialStepRequest>? Steps,
    string? MetaTitle = null,
    string? MetaDescription = null,
    string? Tags = null,
    string? Model3DUrl = null,
    string? Model3DPosterUrl = null
);
