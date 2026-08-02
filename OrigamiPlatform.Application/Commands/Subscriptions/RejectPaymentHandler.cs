using OrigamiPlatform.Application.DTOs.Subscriptions;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Subscriptions;

public class RejectPaymentHandler
{
    private const int MaxAdminNoteLength = 300;

    private readonly ITransactionRepository _transactions;

    public RejectPaymentHandler(ITransactionRepository transactions)
        => _transactions = transactions;

    public async Task<TransactionDto> HandleAsync(RejectPaymentCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.AdminNote))
            throw new DomainException("Admin note is required when rejecting a transaction.");

        if (command.AdminNote.Length > MaxAdminNoteLength)
            throw new DomainException($"Admin note must not exceed {MaxAdminNoteLength} characters.");

        var transaction = await _transactions.GetByIdAsync(command.TransactionId, ct)
            ?? throw new NotFoundException("Transaction not found.");

        if (transaction.Status != TransactionStatus.PendingConfirmation)
            throw new DomainException("This transaction has already been processed.");

        var now = DateTime.UtcNow;

        transaction.Status = TransactionStatus.Rejected;
        transaction.ConfirmedBy = command.AdminId;
        transaction.ConfirmedAt = now;
        transaction.AdminNote = command.AdminNote;
        transaction.UpdatedAt = now;

        await _transactions.UpdateAsync(transaction, ct);

        return transaction.ToDto();
    }
}
