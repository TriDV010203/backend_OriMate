using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public record CreateUserByAdminCommand(Guid ActorId, CreateUserByAdminRequest Request);
