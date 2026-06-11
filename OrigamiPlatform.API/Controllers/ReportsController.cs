using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Reports;
using OrigamiPlatform.Application.DTOs.Reports;
using OrigamiPlatform.Application.Queries.Reports;
using System.Security.Claims;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly SubmitReportHandler _submitReport;
    private readonly HandleReportHandler _handleReport;

    public ReportsController(SubmitReportHandler submitReport, HandleReportHandler handleReport)
    {
        _submitReport = submitReport;
        _handleReport = handleReport;
    }

    // 1. API dành cho User gửi report
    [HttpPost]
    public async Task<IActionResult> SubmitReport([FromBody] SubmitReportRequest request, CancellationToken ct)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out Guid userId))
            return Unauthorized();

        var command = new SubmitReportCommand(userId, request.TargetType, request.TargetId, request.Reason);
        var reportId = await _submitReport.HandleAsync(command, ct);

        return Ok(new { ReportId = reportId });
    }

    // 2. API dành cho Manager xử lý report (AC-05)
    // Giả sử dự án của bạn Role Manager/Admin được cấu hình tên là "Admin" hoặc "Manager"
    [Authorize(Roles = "Manager,Admin")]
    [HttpPost("{id}/handle")]
    public async Task<IActionResult> HandleReport(Guid id, [FromBody] HandleReportRequest request, CancellationToken ct)
    {
        var managerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(managerIdString, out Guid managerId))
            return Unauthorized();

        var command = new HandleReportCommand(id, managerId, request.ActionType);
        await _handleReport.HandleAsync(command, ct);

        return Ok(new { Message = "Report handled successfully." });
    }

    // Đầu file nhớ thêm: using OrigamiPlatform.Application.Queries.Reports;

    [HttpGet("pending")]
    [Authorize(Roles = "Manager,Admin")] // Chỉ Quản lý mới được xem
    public async Task<IActionResult> GetPendingReports(
        [FromServices] GetPendingReportsHandler getReportsHandler,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new GetPendingReportsQuery(page, pageSize);
        var result = await getReportsHandler.HandleAsync(query, ct);

        return Ok(result);
    }
}