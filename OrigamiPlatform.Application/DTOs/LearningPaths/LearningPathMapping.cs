using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.DTOs.LearningPaths;

public static class LearningPathMapping
{
    public static LearningPathDto ToDto(this LearningPath learningPath)
        => new(
            learningPath.Id,
            learningPath.LearningPathModeId,
            learningPath.LearningPathMode.Name,
            learningPath.Title,
            learningPath.Description,
            learningPath.CoverImageUrl,
            learningPath.Status.ToString(),
            learningPath.Items
                .OrderBy(i => i.ItemOrder)
                .Select(i => new LearningPathItemDto(
                    i.ItemOrder,
                    i.TutorialId,
                    i.Tutorial.Title,
                    i.Tutorial.Slug,
                    i.Tutorial.CoverImageUrl,
                    i.Tutorial.Difficulty.ToString(),
                    i.Tutorial.CategoryId,
                    i.Tutorial.Category.Name))
                .ToList(),
            learningPath.CreatedAt,
            learningPath.UpdatedAt,
            learningPath.PublishedAt);

    public static LearningPathListItemDto ToListItemDto(this LearningPath learningPath)
        => new(
            learningPath.Id,
            learningPath.LearningPathModeId,
            learningPath.LearningPathMode.Name,
            learningPath.Title,
            learningPath.Description,
            learningPath.CoverImageUrl,
            learningPath.Status.ToString(),
            learningPath.Items.Count,
            learningPath.CreatedAt,
            learningPath.UpdatedAt,
            learningPath.PublishedAt);
}
