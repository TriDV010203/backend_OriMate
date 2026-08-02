using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrigamiPlatform.Application.Commands.DailyChallenge;

namespace OrigamiPlatform.Infrastructure.BackgroundJobs;

// FT-34: the only cron-like job in the solution. Runs once a day at 00:00 GMT+7 — closes
// yesterday's challenge (ranks submissions, awards Top 3) then activates today's (promotes a
// Scheduled row, or auto-picks a tutorial as a safety net). Each step is isolated in its own
// try/catch so one failing never blocks the other or crashes the host.
public class DailyChallengeSchedulerService : BackgroundService
{
    private static readonly TimeSpan RunAtGmt7 = TimeSpan.Zero; // 00:00

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyChallengeSchedulerService> _logger;

    public DailyChallengeSchedulerService(IServiceScopeFactory scopeFactory, ILogger<DailyChallengeSchedulerService> logger)
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        try
        {
            var closeHandler = scope.ServiceProvider.GetRequiredService<CloseDailyChallengeResultHandler>();
            await closeHandler.HandleAsync(new CloseDailyChallengeResultCommand(today.AddDays(-1)), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DailyChallengeSchedulerService: failed to close challenge for {Date}", today.AddDays(-1));
        }

        try
        {
            var activateHandler = scope.ServiceProvider.GetRequiredService<ActivateDailyChallengeHandler>();
            await activateHandler.HandleAsync(new ActivateDailyChallengeCommand(), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DailyChallengeSchedulerService: failed to activate challenge for {Date}", today);
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
