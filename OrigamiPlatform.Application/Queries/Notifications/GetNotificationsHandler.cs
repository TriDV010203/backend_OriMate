using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.Notifications;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Notifications;

public class GetNotificationsHandler
{
    private readonly INotificationRepository _notificationRepository;

    public GetNotificationsHandler(INotificationRepository notificationRepository)
        => _notificationRepository = notificationRepository;

    public async Task<PagedResult<NotificationDto>> HandleAsync(
        GetNotificationsQuery query, CancellationToken ct = default)
    {
        // Chuẩn hóa dữ liệu phân trang đầu vào giống GetTutorialsHandler
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var pagedNotifications = await _notificationRepository.GetUserNotificationsAsync(query.UserId, page, pageSize, ct);

        if (!pagedNotifications.Items.Any())
        {
            return new PagedResult<NotificationDto>(new List<NotificationDto>(), 0, page, pageSize, 0);
        }

        var dtos = pagedNotifications.Items.Select(n => new NotificationDto(
            Id: n.Id,
            Type: n.Type,
            Message: n.Message,
            EntityType: n.EntityType,
            EntityId: n.EntityId,
            IsRead: n.IsRead,
            CreatedAt: n.CreatedAt
        )).ToList();

        return new PagedResult<NotificationDto>(
            dtos,
            pagedNotifications.TotalCount,
            page,
            pageSize,
            pagedNotifications.TotalPages);
    }
}