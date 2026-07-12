using OrigamiPlatform.Application.Features.AdminConfiguration.Validators;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public class AssignRoleHandler
{
    private readonly IUserRepository _userRepo;
    private readonly IAuditLogRepository _auditLog;

    public AssignRoleHandler(IUserRepository userRepo, IAuditLogRepository auditLog)
        => (_userRepo, _auditLog) = (userRepo, auditLog);

    public async Task HandleAsync(AssignRoleCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        AssignRoleRequestValidator.Validate(req.Role);

        var user = await _userRepo.GetByIdAsync(command.UserId, ct)
            ?? throw new NotFoundException($"User {command.UserId} not found.");

        var roleType = Enum.Parse<UserRoleType>(req.Role);

        if (user.Roles.Any(r => r.Role == roleType))
            throw new ConflictException("User already has this role.");

        var userRole = new UserRole
        {
            UserId = command.UserId,
            Role = roleType,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepo.AddRoleAsync(userRole, ct);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = command.ActorId,
            Action = "AssignRole",
            EntityType = "User",
            EntityId = command.UserId.ToString(),
            OldValue = null,
            NewValue = req.Role,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }
}
