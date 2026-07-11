namespace OrigamiPlatform.Application.Commands.Subscriptions;

public record RejectPaymentCommand(Guid AdminId, Guid TransactionId, string AdminNote);
