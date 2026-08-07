namespace OrigamiPlatform.Application.Queries.Subscriptions;

public record GetTransactionByIdQuery(Guid TransactionId, Guid RequesterId);
