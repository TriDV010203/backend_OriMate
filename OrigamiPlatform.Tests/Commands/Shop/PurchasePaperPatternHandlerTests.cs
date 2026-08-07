using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Shop;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Shop;

public class PurchasePaperPatternHandlerTests
{
    private readonly Mock<IPaperPatternRepository> _mockPatterns;
    private readonly Mock<IUserPaperPatternRepository> _mockUserPatterns;
    private readonly Mock<IHatGapTransactionRepository> _mockHatGapRepo;
    private readonly PurchasePaperPatternHandler _handler;

    public PurchasePaperPatternHandlerTests()
    {
        _mockPatterns = new Mock<IPaperPatternRepository>();
        _mockUserPatterns = new Mock<IUserPaperPatternRepository>();
        _mockHatGapRepo = new Mock<IHatGapTransactionRepository>();

        var hatGapService = new HatGapAwardService(_mockHatGapRepo.Object);
        _handler = new PurchasePaperPatternHandler(_mockPatterns.Object, _mockUserPatterns.Object, hatGapService);
    }

    [Fact]
    public async Task HandleAsync_PatternNotFoundOrInactive_ThrowsNotFoundException()
    {
        var command = new PurchasePaperPatternCommand(Guid.NewGuid(), Guid.NewGuid());
        _mockPatterns.Setup(p => p.GetByIdAsync(command.PaperPatternId, default)).ReturnsAsync((PaperPattern?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_AlreadyOwned_ThrowsDomainException()
    {
        var command = new PurchasePaperPatternCommand(Guid.NewGuid(), Guid.NewGuid());
        var pattern = new PaperPattern { Id = command.PaperPatternId, IsActive = true };

        _mockPatterns.Setup(p => p.GetByIdAsync(command.PaperPatternId, default)).ReturnsAsync(pattern);
        _mockUserPatterns.Setup(p => p.ExistsAsync(command.UserId, command.PaperPatternId, default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("You already own this pattern.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InsufficientBalance_ThrowsDomainException()
    {
        var command = new PurchasePaperPatternCommand(Guid.NewGuid(), Guid.NewGuid());
        var pattern = new PaperPattern { Id = command.PaperPatternId, IsActive = true, PriceInHatGap = 100 }; // Costs 100

        _mockPatterns.Setup(p => p.GetByIdAsync(command.PaperPatternId, default)).ReturnsAsync(pattern);
        _mockUserPatterns.Setup(p => p.ExistsAsync(command.UserId, command.PaperPatternId, default)).ReturnsAsync(false);
        _mockHatGapRepo.Setup(r => r.GetLatestBalanceAsync(command.UserId, default)).ReturnsAsync(50); // Balance is 50 (< 100)

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("Insufficient Hạt Gấp balance.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidPurchase_DeductsBalanceAndAddsPattern()
    {
        var command = new PurchasePaperPatternCommand(Guid.NewGuid(), Guid.NewGuid());
        var pattern = new PaperPattern { Id = command.PaperPatternId, IsActive = true, PriceInHatGap = 50 };

        _mockPatterns.Setup(p => p.GetByIdAsync(command.PaperPatternId, default)).ReturnsAsync(pattern);
        _mockUserPatterns.Setup(p => p.ExistsAsync(command.UserId, command.PaperPatternId, default)).ReturnsAsync(false);
        _mockHatGapRepo.Setup(r => r.GetLatestBalanceAsync(command.UserId, default)).ReturnsAsync(100);

        await _handler.HandleAsync(command);

        // Verify HatGap deduction transaction
        _mockHatGapRepo.Verify(r => r.AddAsync(It.Is<HatGapTransaction>(t =>
            t.UserId == command.UserId &&
            t.Amount == -50 &&
            t.Type == HatGapTransactionType.Spend &&
            t.BalanceAfter == 50
        ), default), Times.Once);

        // Verify UserPattern added
        _mockUserPatterns.Verify(p => p.AddAsync(It.Is<UserPaperPattern>(u =>
            u.UserId == command.UserId &&
            u.PaperPatternId == command.PaperPatternId
        ), default), Times.Once);
    }
}
