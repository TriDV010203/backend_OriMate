using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public record UpdateCategoryCommand(Guid ActorId, int CategoryId, UpdateCategoryRequest Request);
