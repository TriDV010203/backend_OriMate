using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public class ActivateUserHandler
{
    private readonly IUserRepository _userRepo;
    private readonly IAuditLogRepository _auditLog;
    private readonly INotificationService _notifications;

    public ActivateUserHandler(IUserRepository userRepo, IAuditLogRepository auditLog, INotificationService notifications)
        => (_userRepo, _auditLog, _notifications) = (userRepo, auditLog, notifications);

    public async Task HandleAsync(ActivateUserCommand command, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(command.UserId, ct)
            ?? throw new NotFoundException($"User {command.UserId} not found.");

        if (user.Status == AccountStatus.Active)
            throw new BadRequestException("User account is already active.");

        user.Status = AccountStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user, ct);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = command.ActorId,
            Action = "ActivateAccount",
            EntityType = "User",
            EntityId = command.UserId.ToString(),
            OldValue = null,
            NewValue = null,
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _notifications.NotifyUserAsync(
            command.UserId,
            NotificationType.AccountActivated,
            "Your account has been reactivated.",
            "User",
            command.UserId,
            ct);
    }
}
