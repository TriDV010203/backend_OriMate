using OrigamiPlatform.Application.DTOs.Subscriptions;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.Subscriptions;

public class GetTransactionByIdHandler
{
    private readonly ITransactionRepository _transactions;

    public GetTransactionByIdHandler(ITransactionRepository transactions)
        => _transactions = transactions;

    public async Task<TransactionDto> HandleAsync(GetTransactionByIdQuery query, CancellationToken ct = default)
    {
        var transaction = await _transactions.GetByIdAsync(query.TransactionId, ct)
            ?? throw new NotFoundException("Transaction not found.");

        // A buyer may only poll the status of their own transaction.
        if (transaction.UserId != query.RequesterId)
            throw new ForbiddenException("You do not have access to this transaction.");

        return transaction.ToDto();
    }
}
