namespace OrigamiPlatform.Application.DTOs.FamilyProjects;

public record SharedProjectPostDto(
    Guid PostId,
    Guid ProjectId,
    string Content,
    DateTime CreatedAt);
