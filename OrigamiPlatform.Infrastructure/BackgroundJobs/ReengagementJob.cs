using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Infrastructure.BackgroundJobs;

// FT-30: re-engagement email trigger. Runs once a day at 09:00 GMT+7 — finds users whose
// StreakLog.LastActiveDate was exactly 3 days ago and nudges them back with an email. Best-effort:
// the whole run (and each individual send) is wrapped in try/catch so one bad address or a down
// SMTP server never crashes the host or blocks the rest of the batch.
public class ReengagementJob : BackgroundService
{
    private static readonly TimeSpan RunAtGmt7 = TimeSpan.FromHours(9); // 09:00
    private const int InactivityThresholdDays = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReengagementJob> _logger;

    public ReengagementJob(IServiceScopeFactory scopeFactory, ILogger<ReengagementJob> logger)
        => (_scopeFactory, _logger) = (scopeFactory, logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayToNextRun();

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var streakLogs = scope.ServiceProvider.GetRequiredService<IStreakLogRepository>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        try
        {
            var inactiveUsers = await streakLogs.GetUsersInactiveForDaysAsync(InactivityThresholdDays, ct);

            foreach (var (userId, email) in inactiveUsers)
            {
                try
                {
                    await emailService.SendAsync(
                        email,
                        "Bạn ơi, đừng để streak gián đoạn! 🔥",
                        "Đã vài ngày rồi bạn chưa quay lại gấp giấy. Ghé OriMate ngay để tiếp tục chuỗi ngày học của bạn nhé!",
                        ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ReengagementJob: failed to send email to user {UserId}", userId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReengagementJob: failed to query inactive users");
        }
    }

    private static TimeSpan GetDelayToNextRun()
    {
        var nowGmt7 = DateTime.UtcNow.AddHours(7);
        var nextRun = nowGmt7.Date.Add(RunAtGmt7);
        if (nextRun <= nowGmt7)
            nextRun = nextRun.AddDays(1);

        return nextRun - nowGmt7;
    }
}
