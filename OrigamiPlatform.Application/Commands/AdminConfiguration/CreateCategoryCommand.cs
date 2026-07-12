using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public record CreateCategoryCommand(Guid ActorId, CreateCategoryRequest Request);
