namespace OrigamiPlatform.Application.Commands.WeeklyChallenge;

// Internal command driven by the nightly scheduler (closes "yesterday" — a no-op unless
// yesterday was Sunday) or the admin "run now" endpoint for testing.
public record CloseWeeklyChallengeResultCommand(DateOnly ChallengeDate);
