using OrigamiPlatform.Application.DTOs.WeeklyChallenge;

namespace OrigamiPlatform.Application.Commands.WeeklyChallenge;

public record SubmitWeeklyChallengeCommand(Guid UserId, SubmitWeeklyChallengeRequest Request);
