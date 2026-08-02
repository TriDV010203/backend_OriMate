using Moq;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Services;

public class HatGapAwardServiceTests
{
    private readonly Mock<IHatGapTransactionRepository> _transactions = new();

    private HatGapAwardService CreateService() => new(_transactions.Object);

    [Fact]
    public async Task AwardAsync_Earn_AddsToBalance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _transactions.Setup(t => t.GetLatestBalanceAsync(userId, default)).ReturnsAsync(10);

        HatGapTransaction? added = null;
        _transactions.Setup(t => t.AddAsync(It.IsAny<HatGapTransaction>(), default))
            .Callback<HatGapTransaction, CancellationToken>((tx, _) => added = tx)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.AwardAsync(userId, 3, HatGapTransactionType.Earn, "Test", default);

        // Assert
        Assert.NotNull(added);
        Assert.Equal(13, added!.BalanceAfter);
        Assert.Equal(3, added.Amount);
        Assert.Equal(HatGapTransactionType.Earn, added.Type);
        _transactions.Verify(t => t.AddAsync(It.IsAny<HatGapTransaction>(), default), Times.Once);
    }

    [Fact]
    public async Task AwardAsync_FirstEverTransaction_BalanceStartsAtZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _transactions.Setup(t => t.GetLatestBalanceAsync(userId, default)).ReturnsAsync(0);

        HatGapTransaction? added = null;
        _transactions.Setup(t => t.AddAsync(It.IsAny<HatGapTransaction>(), default))
            .Callback<HatGapTransaction, CancellationToken>((tx, _) => added = tx)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.AwardAsync(userId, 3, HatGapTransactionType.Earn, "Test", default);

        // Assert
        Assert.NotNull(added);
        Assert.Equal(3, added!.BalanceAfter);
    }

    [Fact]
    public async Task AwardAsync_Spend_DeductsFromBalance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _transactions.Setup(t => t.GetLatestBalanceAsync(userId, default)).ReturnsAsync(20);

        HatGapTransaction? added = null;
        _transactions.Setup(t => t.AddAsync(It.IsAny<HatGapTransaction>(), default))
            .Callback<HatGapTransaction, CancellationToken>((tx, _) => added = tx)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act — callers pass a negative amount for Spend (see PurchaseStreakFreezeHandler).
        await service.AwardAsync(userId, -20, HatGapTransactionType.Spend, "Test", default);

        // Assert
        Assert.NotNull(added);
        Assert.Equal(0, added!.BalanceAfter);
        Assert.Equal(-20, added.Amount);
        Assert.Equal(HatGapTransactionType.Spend, added.Type);
    }

    [Fact]
    public async Task AwardAsync_InsufficientBalance_ThrowsDomainException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _transactions.Setup(t => t.GetLatestBalanceAsync(userId, default)).ReturnsAsync(5);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => service.AwardAsync(userId, -20, HatGapTransactionType.Spend, "Test", default));

        _transactions.Verify(t => t.AddAsync(It.IsAny<HatGapTransaction>(), default), Times.Never);
    }

    [Fact]
    public async Task AwardAsync_EarnNeverThrows_EvenIfAmountLarge()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _transactions.Setup(t => t.GetLatestBalanceAsync(userId, default)).ReturnsAsync(0);

        HatGapTransaction? added = null;
        _transactions.Setup(t => t.AddAsync(It.IsAny<HatGapTransaction>(), default))
            .Callback<HatGapTransaction, CancellationToken>((tx, _) => added = tx)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var exception = await Record.ExceptionAsync(
            () => service.AwardAsync(userId, 1000, HatGapTransactionType.Earn, "Test", default));

        // Assert
        Assert.Null(exception);
        Assert.NotNull(added);
        Assert.Equal(1000, added!.BalanceAfter);
    }

    [Fact]
    public async Task AwardAsync_CorrectSourceStored()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _transactions.Setup(t => t.GetLatestBalanceAsync(userId, default)).ReturnsAsync(0);

        HatGapTransaction? added = null;
        _transactions.Setup(t => t.AddAsync(It.IsAny<HatGapTransaction>(), default))
            .Callback<HatGapTransaction, CancellationToken>((tx, _) => added = tx)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.AwardAsync(userId, 1, HatGapTransactionType.Earn, "DailyQuestBonus", default);

        // Assert
        Assert.NotNull(added);
        Assert.Equal("DailyQuestBonus", added!.Source);
        Assert.False(string.IsNullOrEmpty(added.Source));
    }
}
