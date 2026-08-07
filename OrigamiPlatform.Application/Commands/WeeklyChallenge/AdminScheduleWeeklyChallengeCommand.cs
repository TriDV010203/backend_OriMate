using OrigamiPlatform.Application.DTOs.WeeklyChallenge;

namespace OrigamiPlatform.Application.Commands.WeeklyChallenge;

public record AdminScheduleWeeklyChallengeCommand(Guid ActorId, ScheduleWeeklyChallengeRequest Request);
