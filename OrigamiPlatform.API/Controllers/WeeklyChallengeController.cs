using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.WeeklyChallenge;
using OrigamiPlatform.Application.DTOs.WeeklyChallenge;
using OrigamiPlatform.Application.Queries.WeeklyChallenge;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

// Thử thách tuần — mirror DailyChallengeController (FT-34): public browsing, user submission,
// và Admin/Manager scheduling. Chỉ mở vào Chủ Nhật (GMT+7) — ngoài ngày đó "current" trả 404.
[ApiController]
[Route("api/weekly-challenge")]
public class WeeklyChallengeController : ControllerBase
{
    private readonly GetCurrentWeeklyChallengeHandler _getCurrent;
    private readonly GetWeeklyChallengeSubmissionsHandler _getSubmissions;
    private readonly SubmitWeeklyChallengeHandler _submit;
    private readonly GetWeeklyChallengeSuggestionsHandler _getSuggestions;
    private readonly AdminScheduleWeeklyChallengeHandler _adminSchedule;
    private readonly GetAdminWeeklyChallengeCalendarHandler _getAdminCalendar;
    private readonly ActivateWeeklyChallengeHandler _activate;
    private readonly CloseWeeklyChallengeResultHandler _closeResult;

    public WeeklyChallengeController(
        GetCurrentWeeklyChallengeHandler getCurrent,
        GetWeeklyChallengeSubmissionsHandler getSubmissions,
        SubmitWeeklyChallengeHandler submit,
        GetWeeklyChallengeSuggestionsHandler getSuggestions,
        AdminScheduleWeeklyChallengeHandler adminSchedule,
        GetAdminWeeklyChallengeCalendarHandler getAdminCalendar,
        ActivateWeeklyChallengeHandler activate,
        CloseWeeklyChallengeResultHandler closeResult)
        => (_getCurrent, _getSubmissions, _submit, _getSuggestions,
            _adminSchedule, _getAdminCalendar, _activate, _closeResult)
            = (getCurrent, getSubmissions, submit, getSuggestions,
                adminSchedule, getAdminCalendar, activate, closeResult);

    /// <summary>GET /api/weekly-challenge/current — Thử thách tuần hiện tại (chỉ có vào Chủ Nhật; công khai, cá nhân hoá nếu có token).</summary>
    [HttpGet("current")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
    {
        var result = await _getCurrent.HandleAsync(new GetCurrentWeeklyChallengeQuery(GetOptionalCurrentUserId()), ct);
        return Ok(result);
    }

    /// <summary>GET /api/weekly-challenge/current/submissions — bài nộp của tuần này, sắp xếp theo lượt thích.</summary>
    [HttpGet("current/submissions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCurrentSubmissions(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var result = await _getSubmissions.HandleAsync(
            new GetWeeklyChallengeSubmissionsQuery(today, page, pageSize, GetOptionalCurrentUserId()), ct);
        return Ok(result);
    }

    /// <summary>POST /api/weekly-challenge/current/submit — nộp ảnh cho Thử thách tuần này.</summary>
    [HttpPost("current/submit")]
    [Authorize]
    public async Task<IActionResult> SubmitCurrent(SubmitWeeklyChallengeRequest request, CancellationToken ct)
    {
        var result = await _submit.HandleAsync(new SubmitWeeklyChallengeCommand(GetCurrentUserId(), request), ct);
        return Ok(result);
    }

    /// <summary>GET /api/weekly-challenge/admin/suggestions — gợi ý tutorial Advanced cho trang lên lịch, không ghi DB.</summary>
    [HttpGet("admin/suggestions")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetSuggestions([FromQuery] int count = 5, CancellationToken ct = default)
    {
        var result = await _getSuggestions.HandleAsync(new GetWeeklyChallengeSuggestionsQuery(count), ct);
        return Ok(result);
    }

    /// <summary>POST /api/weekly-challenge/admin/schedule — đặt lịch tutorial cho một Chủ Nhật (hôm nay hoặc tương lai).</summary>
    [HttpPost("admin/schedule")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Schedule(ScheduleWeeklyChallengeRequest request, CancellationToken ct)
    {
        var result = await _adminSchedule.HandleAsync(
            new AdminScheduleWeeklyChallengeCommand(GetCurrentUserId(), request), ct);
        return Ok(result);
    }

    /// <summary>GET /api/weekly-challenge/admin/calendar — danh sách Thử thách tuần đã lên lịch/đang mở/đã đóng trong khoảng ngày.</summary>
    [HttpGet("admin/calendar")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAdminCalendar(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 31, CancellationToken ct = default)
    {
        var result = await _getAdminCalendar.HandleAsync(
            new GetAdminWeeklyChallengeCalendarQuery(fromDate, toDate, null, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/weekly-challenge/admin/run-scheduler — chạy tay các bước mà job đêm 00:00 GMT+7
    /// thực hiện (đóng Thử thách tuần trước, kích hoạt Thử thách tuần này) — dùng để test mà
    /// không phải đợi tới Chủ Nhật thật. Activate tự no-op nếu hôm nay không phải Chủ Nhật.
    /// </summary>
    [HttpPost("admin/run-scheduler")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RunSchedulerNow(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        await _closeResult.HandleAsync(new CloseWeeklyChallengeResultCommand(today.AddDays(-1)), ct);
        await _activate.HandleAsync(new ActivateWeeklyChallengeCommand(), ct);
        return Ok(new { message = "Scheduler steps executed for yesterday(close) and today(activate)." });
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
            throw new ForbiddenException("Invalid user token.");

        return userId;
    }

    private Guid? GetOptionalCurrentUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
