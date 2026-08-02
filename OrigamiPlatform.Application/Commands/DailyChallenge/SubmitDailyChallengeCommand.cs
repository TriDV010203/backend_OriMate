using OrigamiPlatform.Application.DTOs.DailyChallenge;

namespace OrigamiPlatform.Application.Commands.DailyChallenge;

public record SubmitDailyChallengeCommand(Guid UserId, SubmitDailyChallengeRequest Request);
