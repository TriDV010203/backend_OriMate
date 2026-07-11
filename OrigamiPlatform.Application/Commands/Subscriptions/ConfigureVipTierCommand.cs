namespace OrigamiPlatform.Application.Commands.Subscriptions;

public record ConfigureVipTierCommand(Guid CreatorId, decimal Price, bool IsActive);
