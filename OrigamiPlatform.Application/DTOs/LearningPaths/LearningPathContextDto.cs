namespace OrigamiPlatform.Application.DTOs.LearningPaths;

/// <summary>Tells a tutorial's own page which published learning path (if any) it belongs to,
/// and its position within it — powers the "this tutorial is lesson X/Y of path Z" banner.</summary>
public record LearningPathContextDto(
    Guid PathId,
    string PathTitle,
    int LessonIndex,
    int TotalLessons,
    bool IsLastLesson
);
