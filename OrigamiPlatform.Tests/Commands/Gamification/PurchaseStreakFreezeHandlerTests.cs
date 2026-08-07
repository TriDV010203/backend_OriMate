using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Gamification;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Gamification;

public class PurchaseStreakFreezeHandlerTests
{
    private readonly Mock<IStreakLogRepository> _mockStreakLogs;
    private readonly Mock<IHatGapTransactionRepository> _mockHatGapRepo;
    private readonly PurchaseStreakFreezeHandler _handler;

    public PurchaseStreakFreezeHandlerTests()
    {
        _mockStreakLogs = new Mock<IStreakLogRepository>();
        _mockHatGapRepo = new Mock<IHatGapTransactionRepository>();
        var hatGapService = new HatGapAwardService(_mockHatGapRepo.Object);
        _handler = new PurchaseStreakFreezeHandler(_mockStreakLogs.Object, hatGapService);
    }

    [Fact]
    public async Task HandleAsync_MaxFreezes_ThrowsDomainException()
    {
        var command = new PurchaseStreakFreezeCommand(Guid.NewGuid());
        var streak = new StreakLog { UserId = command.UserId, FreezeCount = 2 };
        
        _mockStreakLogs.Setup(s => s.GetByUserIdAsync(command.UserId, default)).ReturnsAsync(streak);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("maximum of 2 Streak Freezes", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InsufficientBalance_ThrowsDomainException()
    {
        var command = new PurchaseStreakFreezeCommand(Guid.NewGuid());
        var streak = new StreakLog { UserId = command.UserId, FreezeCount = 1 };
        
        _mockStreakLogs.Setup(s => s.GetByUserIdAsync(command.UserId, default)).ReturnsAsync(streak);
        _mockHatGapRepo.Setup(r => r.GetLatestBalanceAsync(command.UserId, default)).ReturnsAsync(10); // cost is 20

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("Insufficient Hạt Gấp balance.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_DeductsBalanceAndIncrementsFreezeCount()
    {
        var command = new PurchaseStreakFreezeCommand(Guid.NewGuid());
        var streak = new StreakLog { UserId = command.UserId, FreezeCount = 1, CurrentStreak = 5, LongestStreak = 10 };
        
        _mockStreakLogs.Setup(s => s.GetByUserIdAsync(command.UserId, default)).ReturnsAsync(streak);
        _mockHatGapRepo.Setup(r => r.GetLatestBalanceAsync(command.UserId, default)).ReturnsAsync(100);

        var result = await _handler.HandleAsync(command);

        Assert.Equal(2, result.FreezeCount);
        Assert.Equal(5, result.CurrentStreak);
        Assert.Equal(10, result.LongestStreak);

        _mockHatGapRepo.Verify(r => r.AddAsync(It.Is<HatGapTransaction>(t => 
            t.UserId == command.UserId && 
            t.Amount == -20 && 
            t.Type == HatGapTransactionType.Spend),
            default), Times.Once);

        _mockStreakLogs.Verify(s => s.UpdateAsync(It.Is<StreakLog>(sl => sl.FreezeCount == 2), default), Times.Once);
    }
}
