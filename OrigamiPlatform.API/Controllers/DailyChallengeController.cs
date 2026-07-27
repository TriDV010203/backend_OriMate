using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.DailyChallenge;
using OrigamiPlatform.Application.DTOs.DailyChallenge;
using OrigamiPlatform.Application.Queries.DailyChallenge;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

// FT-34: Daily Challenge — public browsing endpoints, user submission, and Admin/Manager scheduling.
[ApiController]
[Route("api/daily-challenge")]
public class DailyChallengeController : ControllerBase
{
    private readonly GetTodayChallengeHandler _getToday;
    private readonly GetChallengeSubmissionsHandler _getSubmissions;
    private readonly SubmitDailyChallengeHandler _submit;
    private readonly GetChallengeStreakLeaderboardHandler _getLeaderboard;
    private readonly GetChallengeResultHandler _getResult;
    private readonly GetChallengeSuggestionsHandler _getSuggestions;
    private readonly AdminScheduleDailyChallengeHandler _adminSchedule;
    private readonly GetAdminChallengeCalendarHandler _getAdminCalendar;
    private readonly ActivateDailyChallengeHandler _activate;
    private readonly CloseDailyChallengeResultHandler _closeResult;

    public DailyChallengeController(
        GetTodayChallengeHandler getToday,
        GetChallengeSubmissionsHandler getSubmissions,
        SubmitDailyChallengeHandler submit,
        GetChallengeStreakLeaderboardHandler getLeaderboard,
        GetChallengeResultHandler getResult,
        GetChallengeSuggestionsHandler getSuggestions,
        AdminScheduleDailyChallengeHandler adminSchedule,
        GetAdminChallengeCalendarHandler getAdminCalendar,
        ActivateDailyChallengeHandler activate,
        CloseDailyChallengeResultHandler closeResult)
        => (_getToday, _getSubmissions, _submit, _getLeaderboard, _getResult, _getSuggestions,
            _adminSchedule, _getAdminCalendar, _activate, _closeResult)
            = (getToday, getSubmissions, submit, getLeaderboard, getResult, getSuggestions,
                adminSchedule, getAdminCalendar, activate, closeResult);

    /// <summary>GET /api/daily-challenge/today — today's challenge (public; personalized if logged in).</summary>
    [HttpGet("today")]
    [AllowAnonymous]
    public async Task<IActionResult> GetToday(CancellationToken ct)
    {
        var result = await _getToday.HandleAsync(new GetTodayChallengeQuery(GetOptionalCurrentUserId()), ct);
        return Ok(result);
    }

    /// <summary>GET /api/daily-challenge/today/submissions — today's submissions, sorted by like count.</summary>
    [HttpGet("today/submissions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTodaySubmissions(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var result = await _getSubmissions.HandleAsync(
            new GetChallengeSubmissionsQuery(today, page, pageSize, GetOptionalCurrentUserId()), ct);
        return Ok(result);
    }

    /// <summary>POST /api/daily-challenge/today/submit — submit a photo to today's challenge.</summary>
    [HttpPost("today/submit")]
    [Authorize]
    public async Task<IActionResult> SubmitToday(SubmitDailyChallengeRequest request, CancellationToken ct)
    {
        var result = await _submit.HandleAsync(new SubmitDailyChallengeCommand(GetCurrentUserId(), request), ct);
        return Ok(result);
    }

    /// <summary>GET /api/daily-challenge/leaderboard — top Challenge Streak users.</summary>
    [HttpGet("leaderboard")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int top = 20, CancellationToken ct = default)
    {
        var result = await _getLeaderboard.HandleAsync(new GetChallengeStreakLeaderboardQuery(top), ct);
        return Ok(result);
    }

    /// <summary>GET /api/daily-challenge/{date}/result — closed-challenge result (top 3 + total participants).</summary>
    [HttpGet("{date}/result")]
    [AllowAnonymous]
    public async Task<IActionResult> GetResult(DateOnly date, CancellationToken ct)
    {
        var result = await _getResult.HandleAsync(new GetChallengeResultQuery(date), ct);
        return Ok(result);
    }

    /// <summary>GET /api/daily-challenge/admin/suggestions — read-only candidate tutorials for the schedule screen.</summary>
    [HttpGet("admin/suggestions")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetSuggestions([FromQuery] int count = 5, CancellationToken ct = default)
    {
        var result = await _getSuggestions.HandleAsync(new GetChallengeSuggestionsQuery(count), ct);
        return Ok(result);
    }

    /// <summary>POST /api/daily-challenge/admin/schedule — schedule a tutorial for a given (today or future) date.</summary>
    [HttpPost("admin/schedule")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Schedule(ScheduleDailyChallengeRequest request, CancellationToken ct)
    {
        var result = await _adminSchedule.HandleAsync(
            new AdminScheduleDailyChallengeCommand(GetCurrentUserId(), request), ct);
        return Ok(result);
    }

    /// <summary>GET /api/daily-challenge/admin/calendar — scheduled/active/closed challenges in a date range.</summary>
    [HttpGet("admin/calendar")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAdminCalendar(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 31, CancellationToken ct = default)
    {
        var result = await _getAdminCalendar.HandleAsync(
            new GetAdminChallengeCalendarQuery(fromDate, toDate, null, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/daily-challenge/admin/run-scheduler — manually runs the same steps as the nightly
    /// DailyChallengeSchedulerService (close yesterday, activate today). For testing only, so a
    /// developer/tester doesn't have to wait for the 00:00 GMT+7 job to verify the flow end-to-end.
    /// </summary>
    [HttpPost("admin/run-scheduler")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RunSchedulerNow(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        await _closeResult.HandleAsync(new CloseDailyChallengeResultCommand(today.AddDays(-1)), ct);
        await _activate.HandleAsync(new ActivateDailyChallengeCommand(), ct);
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
