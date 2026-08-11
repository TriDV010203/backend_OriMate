using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Common;

public class SafeNotificationService : INotificationService
{
    private readonly INotificationService _innerService;

    // Không cần ILogger ở đây nữa
    public SafeNotificationService(INotificationService innerService)
    {
        _innerService = innerService;
    }

    public async Task NotifyUserAsync(
        Guid userId,
        NotificationType type,
        string message,
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
    {
        try
        {
            await _innerService.NotifyUserAsync(userId, type, message, entityType, entityId, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Notification Error] User: {userId}, Type: {type}. Lỗi: {ex.Message}");
        }
    }

    public async Task NotifyUsersWithRoleAsync(
        UserRoleType role,
        NotificationType type,
        string message,
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
    {
        try
        {
            await _innerService.NotifyUsersWithRoleAsync(role, type, message, entityType, entityId, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Notification Error] Role: {role}, Type: {type}. Lỗi: {ex.Message}");
        }
    }
}