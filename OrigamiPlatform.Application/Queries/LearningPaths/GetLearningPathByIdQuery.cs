namespace OrigamiPlatform.Application.Queries.LearningPaths;

/// <summary>Public detail: Published only — 404s otherwise (Draft/Archived aren't public).</summary>
public record GetLearningPathByIdQuery(Guid Id);
