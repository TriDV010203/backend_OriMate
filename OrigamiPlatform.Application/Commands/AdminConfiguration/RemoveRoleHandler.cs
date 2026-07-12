using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public class RemoveRoleHandler
{
    private readonly IUserRepository _userRepo;
    private readonly IAuditLogRepository _auditLog;

    public RemoveRoleHandler(IUserRepository userRepo, IAuditLogRepository auditLog)
        => (_userRepo, _auditLog) = (userRepo, auditLog);

    public async Task HandleAsync(RemoveRoleCommand command, CancellationToken ct = default)
    {
        var req = command.Request;

        if (string.IsNullOrWhiteSpace(req.Role))
            throw new DomainException("Role is required.");

        if (!Enum.TryParse<UserRoleType>(req.Role, out var roleType))
            throw new DomainException($"Invalid role: {req.Role}.");

        if (roleType == UserRoleType.User)
            throw new BadRequestException("Cannot remove the base User role.");

        if (roleType == UserRoleType.Admin && command.ActorId == command.UserId)
            throw new BadRequestException("Cannot remove your own Admin role.");

        var user = await _userRepo.GetByIdAsync(command.UserId, ct)
            ?? throw new NotFoundException($"User {command.UserId} not found.");

        if (!user.Roles.Any(r => r.Role == roleType))
            throw new NotFoundException($"User does not have the {req.Role} role.");

        await _userRepo.RemoveRoleAsync(command.UserId, roleType, ct);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = command.ActorId,
            Action = "RemoveRole",
            EntityType = "User",
            EntityId = command.UserId.ToString(),
            OldValue = req.Role,
            NewValue = null,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }
}
