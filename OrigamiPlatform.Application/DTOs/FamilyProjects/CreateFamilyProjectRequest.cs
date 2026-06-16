namespace OrigamiPlatform.Application.DTOs.FamilyProjects;

public record CreateFamilyProjectRequest(
    Guid TutorialId,
    string Name);
