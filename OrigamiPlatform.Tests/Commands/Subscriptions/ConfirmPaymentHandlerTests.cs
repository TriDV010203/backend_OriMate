using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Subscriptions;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Subscriptions;

public class ConfirmPaymentHandlerTests
{
    private readonly Mock<ITransactionRepository> _mockTransactions;
    private readonly Mock<IVipSubscriptionRepository> _mockSubscriptions;
    private readonly ConfirmPaymentHandler _handler;

    public ConfirmPaymentHandlerTests()
    {
        _mockTransactions = new Mock<ITransactionRepository>();
        _mockSubscriptions = new Mock<IVipSubscriptionRepository>();
        _handler = new ConfirmPaymentHandler(_mockTransactions.Object, _mockSubscriptions.Object);
    }

    [Fact]
    public async Task HandleAsync_TransactionNotFound_ThrowsNotFoundException()
    {
        var command = new ConfirmPaymentCommand(Guid.NewGuid(), Guid.NewGuid());
        _mockTransactions.Setup(t => t.GetByIdAsync(command.TransactionId, default)).ReturnsAsync((Transaction?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Equal("Transaction not found.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_TransactionAlreadyProcessed_ThrowsDomainException()
    {
        var command = new ConfirmPaymentCommand(Guid.NewGuid(), Guid.NewGuid());
        var transaction = new Transaction { Id = command.TransactionId, Status = TransactionStatus.Confirmed };
        _mockTransactions.Setup(t => t.GetByIdAsync(command.TransactionId, default)).ReturnsAsync(transaction);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("This transaction has already been processed.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_MissingCreatorId_ThrowsDomainException()
    {
        var command = new ConfirmPaymentCommand(Guid.NewGuid(), Guid.NewGuid());
        var transaction = new Transaction { Id = command.TransactionId, Status = TransactionStatus.PendingConfirmation, CreatorId = null };
        _mockTransactions.Setup(t => t.GetByIdAsync(command.TransactionId, default)).ReturnsAsync(transaction);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("Transaction is missing creator information.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidTransaction_ConfirmsAndCreatesSubscription()
    {
        var command = new ConfirmPaymentCommand(Guid.NewGuid(), Guid.NewGuid());
        var transaction = new Transaction 
        { 
            Id = command.TransactionId, 
            Status = TransactionStatus.PendingConfirmation, 
            CreatorId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };
        _mockTransactions.Setup(t => t.GetByIdAsync(command.TransactionId, default)).ReturnsAsync(transaction);

        var result = await _handler.HandleAsync(command);

        Assert.Equal(TransactionStatus.Confirmed, transaction.Status);
        Assert.Equal(command.AdminId, transaction.ConfirmedBy);
        Assert.NotNull(transaction.ConfirmedAt);
        _mockTransactions.Verify(t => t.UpdateAsync(transaction, default), Times.Once);

        _mockSubscriptions.Verify(s => s.AddAsync(It.Is<VipSubscription>(sub => 
            sub.SubscriberId == transaction.UserId &&
            sub.CreatorId == transaction.CreatorId &&
            sub.TransactionId == transaction.Id &&
            sub.Status == SubscriptionStatus.Active &&
            (sub.EndDate - sub.StartDate).TotalDays == 30
        ), default), Times.Once);
        
        Assert.NotNull(result);
        Assert.Equal(SubscriptionStatus.Active.ToString(), result.Status);
    }
}
