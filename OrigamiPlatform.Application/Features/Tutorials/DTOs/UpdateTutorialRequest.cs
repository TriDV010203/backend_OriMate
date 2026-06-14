namespace OrigamiPlatform.Application.Features.Tutorials.DTOs;

public record UpdateTutorialRequest(
    string Title,
    string Description,
    int CategoryId,
    string? Difficulty,
    string Type,
    string? CoverImageUrl,
    List<CreateTutorialStepRequest>? Steps
);
