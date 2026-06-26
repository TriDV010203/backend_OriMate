namespace OrigamiPlatform.Application.Features.Tutorials.DTOs;

public record TutorialReviewItemResponse(
    Guid Id,
    string Title,
    string Slug,
    string AuthorName,
    int StepCount,
    DateTime CreatedAt
);
