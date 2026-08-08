using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Subscriptions;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Subscriptions;

public class SubscribeHandlerTests
{
    private readonly Mock<ICreatorVipSettingsRepository> _mockSettings;
    private readonly Mock<ITransactionRepository> _mockTransactions;
    private readonly Mock<IVipSubscriptionRepository> _mockSubscriptions;
    private readonly SubscribeHandler _handler;

    public SubscribeHandlerTests()
    {
        _mockSettings = new Mock<ICreatorVipSettingsRepository>();
        _mockTransactions = new Mock<ITransactionRepository>();
        _mockSubscriptions = new Mock<IVipSubscriptionRepository>();
        _handler = new SubscribeHandler(_mockSettings.Object, _mockTransactions.Object, _mockSubscriptions.Object);
    }

    [Fact]
    public async Task HandleAsync_EmptyReferenceCode_ThrowsDomainException()
    {
        var command = new SubscribeCommand(Guid.NewGuid(), Guid.NewGuid(), "");

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("Reference code is required.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ReferenceCodeTooLong_ThrowsDomainException()
    {
        var command = new SubscribeCommand(Guid.NewGuid(), Guid.NewGuid(), new string('a', 101));

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("Reference code must not exceed 100 characters.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_CreatorDoesNotOfferVip_ThrowsNotFoundException()
    {
        var command = new SubscribeCommand(Guid.NewGuid(), Guid.NewGuid(), "REF123");
        _mockSettings.Setup(s => s.GetByCreatorIdAsync(command.CreatorId, default)).ReturnsAsync((CreatorVipSettings?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Equal("This creator does not offer VIP subscriptions.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_CreatorVipNotActive_ThrowsDomainException()
    {
        var command = new SubscribeCommand(Guid.NewGuid(), Guid.NewGuid(), "REF123");
        _mockSettings.Setup(s => s.GetByCreatorIdAsync(command.CreatorId, default)).ReturnsAsync(new CreatorVipSettings { IsActive = false });

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("This creator's VIP subscription is not currently active.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_UserAlreadySubscribed_ThrowsDomainException()
    {
        var command = new SubscribeCommand(Guid.NewGuid(), Guid.NewGuid(), "REF123");
        _mockSettings.Setup(s => s.GetByCreatorIdAsync(command.CreatorId, default)).ReturnsAsync(new CreatorVipSettings { IsActive = true });
        _mockSubscriptions.Setup(s => s.HasActiveSubscriptionAsync(command.SubscriberId, command.CreatorId, default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("You already have an active VIP subscription with this Creator.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_CreatesPendingTransaction()
    {
        var command = new SubscribeCommand(Guid.NewGuid(), Guid.NewGuid(), "REF123");
        _mockSettings.Setup(s => s.GetByCreatorIdAsync(command.CreatorId, default)).ReturnsAsync(new CreatorVipSettings { IsActive = true });
        _mockSubscriptions.Setup(s => s.HasActiveSubscriptionAsync(command.SubscriberId, command.CreatorId, default)).ReturnsAsync(false);

        var result = await _handler.HandleAsync(command);

        _mockTransactions.Verify(t => t.AddAsync(It.Is<Transaction>(x => 
            x.UserId == command.SubscriberId &&
            x.CreatorId == command.CreatorId &&
            x.TransactionType == TransactionType.VipSubscription &&
            x.Amount == VipConstants.FixedPriceVnd &&
            x.Status == TransactionStatus.PendingConfirmation &&
            x.ReferenceCode == command.ReferenceCode
        ), default), Times.Once);

        Assert.NotNull(result);
        Assert.Equal(TransactionStatus.PendingConfirmation.ToString(), result.Status);
    }
}
