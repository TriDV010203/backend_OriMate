using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Reports;

public class SubmitReportHandler
{
    private readonly IReportRepository _reports;

    public SubmitReportHandler(IReportRepository reports)
        => _reports = reports;

    public async Task<Guid> HandleAsync(SubmitReportCommand cmd, CancellationToken ct = default)
    {
        // 1. Kiểm tra duplicate report (NAC-02: User cannot report the same item more than once)
        var hasReported = await _reports.HasUserReportedItemAsync(cmd.ReporterId, cmd.TargetId, cmd.TargetType);
        if (hasReported)
        {
            throw new DomainException("You have already reported this item.");
        }

        // 2. Tạo entity Report
        var report = new Report
        {
            Id = Guid.NewGuid(),
            ReporterId = cmd.ReporterId,
            TargetType = cmd.TargetType,
            TargetId = cmd.TargetId,
            Reason = cmd.Reason,
            Status = ReportStatus.Pending, // Giả sử enum có trạng thái Pending
            CreatedAt = DateTime.UtcNow
        };

        // 3. Lưu database
        await _reports.AddAsync(report);

        // Lưu ý: Việc notify cho Manager (AC-04) có thể được xử lý qua Event/SignalR, 
        // nhưng ở mức độ API, lưu thành công vào DB (queue của Manager) là đã hoàn thành.

        return report.Id;
    }
}