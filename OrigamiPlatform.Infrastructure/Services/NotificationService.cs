using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Infrastructure.Persistence;

namespace OrigamiPlatform.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IUserRepository _userRepo;

    public NotificationService(AppDbContext db, IUserRepository userRepo)
        => (_db, _userRepo) = (db, userRepo);

    public async Task NotifyUsersWithRoleAsync(
        UserRoleType role,
        NotificationType type,
        string message,
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
    {
        var users = await _userRepo.GetUsersByRoleAsync(role, ct);
        if (users.Count == 0) return;

        var now = DateTime.UtcNow;
        var notifications = users.Select(u => new Notification
        {
            Id = Guid.NewGuid(),
            RecipientId = u.Id,
            Type = type,
            Message = message,
            EntityType = entityType,
            EntityId = entityId,
            IsRead = false,
            CreatedAt = now
        });

        await _db.Notifications.AddRangeAsync(notifications, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task NotifyUserAsync(
        Guid userId,
        NotificationType type,
        string message,
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
    {
        //var notification = new Notification
        //{
        //    Id = Guid.NewGuid(),
        //    RecipientId = userId,
        //    Type = type,
        //    Message = message,
        //    EntityType = entityType,
        //    EntityId = entityId,
        //    IsRead = false,
        //    CreatedAt = DateTime.UtcNow
        //};

        //_db.Notifications.Add(notification);
        //await _db.SaveChangesAsync(ct);

        var existing = await _db.Notifications.FirstOrDefaultAsync(
            n => n.RecipientId == userId
              && n.Type == type
              && n.EntityType == entityType
              && n.EntityId == entityId,
            ct);

        if (existing != null)
        {
            existing.Message = message;
            existing.IsRead = false;
            existing.CreatedAt = DateTime.UtcNow;

            _db.Notifications.Update(existing);
        }
        else
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                RecipientId = userId,
                Type = type,
                Message = message,
                EntityType = entityType,
                EntityId = entityId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _db.Notifications.Add(notification);
        }

        await _db.SaveChangesAsync(ct);
    }
}
