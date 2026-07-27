using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.Subscriptions;

public record GetAllTransactionsQuery(TransactionStatus? Status, int Page, int PageSize);
