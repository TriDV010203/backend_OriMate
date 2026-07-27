using OrigamiPlatform.Application.DTOs.DailyChallenge;

namespace OrigamiPlatform.Application.Commands.DailyChallenge;

public record AdminScheduleDailyChallengeCommand(Guid ActorId, ScheduleDailyChallengeRequest Request);
