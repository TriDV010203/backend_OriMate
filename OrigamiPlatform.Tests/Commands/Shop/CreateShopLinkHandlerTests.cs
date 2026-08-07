using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Shop;
using OrigamiPlatform.Application.DTOs.Shop;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Shop;

public class CreateShopLinkHandlerTests
{
    private readonly Mock<IShopLinkRepository> _mockShopLinks;
    private readonly Mock<IAuditLogRepository> _mockAuditLog;
    private readonly CreateShopLinkHandler _handler;

    public CreateShopLinkHandlerTests()
    {
        _mockShopLinks = new Mock<IShopLinkRepository>();
        _mockAuditLog = new Mock<IAuditLogRepository>();
        _handler = new CreateShopLinkHandler(_mockShopLinks.Object, _mockAuditLog.Object);
    }

    [Fact]
    public async Task HandleAsync_InvalidRequest_ThrowsDomainException()
    {
        var req = new CreateShopLinkRequest(string.Empty, "invalid_url", null, null); // Invalid title and URL
        var command = new CreateShopLinkCommand(Guid.NewGuid(), req);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Title is required.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_CreatesLinkAndLogsAudit()
    {
        var req = new CreateShopLinkRequest("Origami Paper", "https://example.com/shop", "https://example.com/img.jpg", "Paper");
        var command = new CreateShopLinkCommand(Guid.NewGuid(), req);

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(req.Title, result.Title);
        Assert.Equal(req.Url, result.Url);
        Assert.True(result.IsActive);

        _mockShopLinks.Verify(r => r.AddAsync(It.Is<ShopLink>(l => l.Title == req.Title && l.Url == req.Url), default), Times.Once);
        _mockAuditLog.Verify(l => l.LogAsync(It.Is<AuditLog>(a => 
            a.ActorId == command.ActorId && 
            a.Action == "CreateShopLink" && 
            a.NewValue == req.Title), default), Times.Once);
    }
}
