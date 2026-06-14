using OrigamiPlatform.Application.DTOs.FamilyProjects;

namespace OrigamiPlatform.Application.Commands.FamilyProjects;

public record CreateFamilyProjectCommand(Guid OwnerId, CreateFamilyProjectRequest Request);
