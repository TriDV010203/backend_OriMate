using OrigamiPlatform.Application.DTOs.Reports;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Reports;

public class GetPendingReportsHandler
{
    private readonly IReportRepository _reports;

    public GetPendingReportsHandler(IReportRepository reports)
        => _reports = reports;

    public async Task<List<PendingReportDto>> HandleAsync(GetPendingReportsQuery query, CancellationToken ct = default)
    {
        var skip = (query.Page - 1) * query.PageSize;
        var reports = await _reports.GetPendingReportsAsync(skip, query.PageSize);

        return reports.Select(r => new PendingReportDto(
            Id: r.Id,
            ReporterId: r.ReporterId,
            TargetType: r.TargetType,
            TargetId: r.TargetId,
            Reason: r.Reason,
            CreatedAt: r.CreatedAt
        )).ToList();
    }
}