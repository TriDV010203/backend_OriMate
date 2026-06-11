using OrigamiPlatform.Application.DTOs.Reports;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Reports;

public class HandleReportHandler
{
    private readonly IReportRepository _reports;
    private readonly ICommunityPostRepository _posts;

    public HandleReportHandler(
        IReportRepository reports,
        ICommunityPostRepository posts)
    {
        _reports = reports;
        _posts = posts;
    }

    public async Task HandleAsync(HandleReportCommand cmd, CancellationToken ct = default)
    {
        // 1. Lấy report từ DB
        var report = await _reports.GetByIdAsync(cmd.ReportId);
        if (report == null)
            throw new NotFoundException("Report not found.");

        if (report.Status != ReportStatus.Pending)
            throw new DomainException("This report has already been handled.");

        // 2. Thực thi Action của Manager
        switch (cmd.ActionType)
        {
            case ReportActionType.Dismiss:
                // Không làm gì với content, chỉ cập nhật trạng thái report thành Bỏ qua
                report.Status = ReportStatus.Dismissed;
                break;

            case ReportActionType.RemoveContent:
                // Ẩn/Xóa content (AC-05)
                await RemoveContentAsync(report.TargetType, report.TargetId);
                // Đánh dấu là đã xử lý
                report.Status = ReportStatus.Reviewed;
                break;

            case ReportActionType.SuspendAccount:
                // Tương lai: Logic khóa tài khoản (Suspend User)
                // Đánh dấu là đã xử lý
                report.Status = ReportStatus.Reviewed;
                break;
        }

        // 3. Cập nhật Audit Log cho Report (Identity, Action, Timestamp)
        report.HandledBy = cmd.ManagerId;
        report.HandledAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        await _reports.UpdateAsync(report);
    }

    // Hàm phụ trợ ẩn bài viết
    private async Task RemoveContentAsync(TargetType targetType, Guid targetId)
    {
        if (targetType == TargetType.CommunityPost)
        {
            var post = await _posts.GetByIdAsync(targetId);
            if (post != null)
            {
                post.IsVisible = false;
                post.IsDeleted = true;
            }
        }
    }
}