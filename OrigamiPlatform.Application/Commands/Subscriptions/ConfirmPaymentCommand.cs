namespace OrigamiPlatform.Application.Commands.Subscriptions;

public record ConfirmPaymentCommand(Guid AdminId, Guid TransactionId);
