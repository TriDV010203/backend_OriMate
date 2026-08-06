namespace OrigamiPlatform.Application.Commands.WeeklyChallenge;

// Internal command driven by the nightly scheduler or the admin "run now" endpoint — no request
// payload, always operates on "today" (GMT+7). No-ops unless today is Sunday.
public record ActivateWeeklyChallengeCommand;
