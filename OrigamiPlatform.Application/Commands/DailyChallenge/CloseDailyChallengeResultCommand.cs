namespace OrigamiPlatform.Application.Commands.DailyChallenge;

// FT-34: internal command driven by DailyChallengeSchedulerService (nightly, closes "yesterday")
// or the admin "run now" endpoint for testing.
public record CloseDailyChallengeResultCommand(DateOnly ChallengeDate);
