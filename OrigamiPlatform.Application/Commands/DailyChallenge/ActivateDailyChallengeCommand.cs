namespace OrigamiPlatform.Application.Commands.DailyChallenge;

// FT-34: internal command driven by DailyChallengeSchedulerService (nightly) or the admin
// "run now" endpoint — no request payload, always operates on "today" (GMT+7).
public record ActivateDailyChallengeCommand;
