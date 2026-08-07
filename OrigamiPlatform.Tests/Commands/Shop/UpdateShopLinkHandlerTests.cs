using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Shop;
using OrigamiPlatform.Application.DTOs.Shop;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Shop;

public class UpdateShopLinkHandlerTests
{
    private readonly Mock<IShopLinkRepository> _mockShopLinks;
    private readonly Mock<IAuditLogRepository> _mockAuditLog;
    private readonly UpdateShopLinkHandler _handler;

    public UpdateShopLinkHandlerTests()
    {
        _mockShopLinks = new Mock<IShopLinkRepository>();
        _mockAuditLog = new Mock<IAuditLogRepository>();
        _handler = new UpdateShopLinkHandler(_mockShopLinks.Object, _mockAuditLog.Object);
    }

    [Fact]
    public async Task HandleAsync_InvalidRequest_ThrowsDomainException()
    {
        var req = new UpdateShopLinkRequest(string.Empty, "invalid_url", null, null, true); // Invalid title and URL
        var command = new UpdateShopLinkCommand(Guid.NewGuid(), Guid.NewGuid(), req);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Title is required.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_LinkNotFound_ThrowsNotFoundException()
    {
        var req = new UpdateShopLinkRequest("Origami Paper", "https://example.com/shop", null, null, true);
        var command = new UpdateShopLinkCommand(Guid.NewGuid(), Guid.NewGuid(), req);

        _mockShopLinks.Setup(r => r.GetByIdAsync(command.ShopLinkId, default)).ReturnsAsync((ShopLink?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Equal("Shop link not found.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_UpdatesLinkAndLogsAudit()
    {
        var req = new UpdateShopLinkRequest("Updated Paper", "https://example.com/shop-new", "img.jpg", "Category", false);
        var command = new UpdateShopLinkCommand(Guid.NewGuid(), Guid.NewGuid(), req);

        var existingLink = new ShopLink { Id = command.ShopLinkId, Title = "Old Paper", Url = "old_url", IsActive = true };

        _mockShopLinks.Setup(r => r.GetByIdAsync(command.ShopLinkId, default)).ReturnsAsync(existingLink);

        var result = await _handler.HandleAsync(command);

        Assert.Equal(req.Title, result.Title);
        Assert.Equal(req.Url, result.Url);
        Assert.False(result.IsActive);

        _mockShopLinks.Verify(r => r.UpdateAsync(It.Is<ShopLink>(l => l.Title == req.Title && l.Url == req.Url), default), Times.Once);
        _mockAuditLog.Verify(l => l.LogAsync(It.Is<AuditLog>(a =>
            a.ActorId == command.ActorId &&
            a.Action == "UpdateShopLink" &&
            a.OldValue == "Old Paper" &&
            a.NewValue == req.Title), default), Times.Once);
    }
}
