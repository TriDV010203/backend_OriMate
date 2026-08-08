using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Subscriptions;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Subscriptions;

public class RejectPaymentHandlerTests
{
    private readonly Mock<ITransactionRepository> _mockTransactions;
    private readonly RejectPaymentHandler _handler;

    public RejectPaymentHandlerTests()
    {
        _mockTransactions = new Mock<ITransactionRepository>();
        _handler = new RejectPaymentHandler(_mockTransactions.Object);
    }

    [Fact]
    public async Task HandleAsync_EmptyNote_ThrowsDomainException()
    {
        var command = new RejectPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), "");

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("Admin note is required when rejecting a transaction.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_NoteTooLong_ThrowsDomainException()
    {
        var command = new RejectPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), new string('a', 301));

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("Admin note must not exceed 300 characters.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_TransactionNotFound_ThrowsNotFoundException()
    {
        var command = new RejectPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), "Invalid payment");
        _mockTransactions.Setup(t => t.GetByIdAsync(command.TransactionId, default)).ReturnsAsync((Transaction?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Equal("Transaction not found.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_TransactionAlreadyProcessed_ThrowsDomainException()
    {
        var command = new RejectPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), "Invalid payment");
        var transaction = new Transaction { Id = command.TransactionId, Status = TransactionStatus.Confirmed };
        _mockTransactions.Setup(t => t.GetByIdAsync(command.TransactionId, default)).ReturnsAsync(transaction);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("This transaction has already been processed.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidTransaction_RejectsPayment()
    {
        var command = new RejectPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), "Invalid payment");
        var transaction = new Transaction { Id = command.TransactionId, Status = TransactionStatus.PendingConfirmation };
        _mockTransactions.Setup(t => t.GetByIdAsync(command.TransactionId, default)).ReturnsAsync(transaction);

        var result = await _handler.HandleAsync(command);

        Assert.Equal(TransactionStatus.Rejected, transaction.Status);
        Assert.Equal(command.AdminId, transaction.ConfirmedBy);
        Assert.NotNull(transaction.ConfirmedAt);
        Assert.Equal("Invalid payment", transaction.AdminNote);
        _mockTransactions.Verify(t => t.UpdateAsync(transaction, default), Times.Once);
        Assert.Equal(TransactionStatus.Rejected.ToString(), result.Status);
    }
}
