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
        var hasReported = await _reports.HasUserReportedItemAsync(cmd.ReporterId, cmd.TargetId, cmd.TargetType);
        if (hasReported)
        {
            throw new DomainException("You have already reported this item.");
        }

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ReporterId = cmd.ReporterId,
            TargetType = cmd.TargetType,
            TargetId = cmd.TargetId,
            Reason = cmd.Reason,
            Status = ReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _reports.AddAsync(report);

        return report.Id;
    }
}