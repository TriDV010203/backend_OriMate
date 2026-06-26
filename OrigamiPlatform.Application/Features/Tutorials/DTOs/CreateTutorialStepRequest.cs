namespace OrigamiPlatform.Application.Features.Tutorials.DTOs;

public record CreateTutorialStepRequest(
    int StepOrder,
    string Description,
    string? ImageUrl
);
