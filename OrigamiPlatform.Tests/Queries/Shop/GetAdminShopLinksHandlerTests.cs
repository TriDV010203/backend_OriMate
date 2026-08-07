using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.DTOs.Shop;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Application.Queries.Shop;
using OrigamiPlatform.Domain.Entities;
using Xunit;

namespace OrigamiPlatform.Tests.Queries.Shop;

public class GetAdminShopLinksHandlerTests
{
    private readonly Mock<IShopLinkRepository> _shopLinkRepositoryMock;
    private readonly GetAdminShopLinksHandler _handler;

    public GetAdminShopLinksHandlerTests()
    {
        _shopLinkRepositoryMock = new Mock<IShopLinkRepository>();
        _handler = new GetAdminShopLinksHandler(_shopLinkRepositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAllShopLinks_WhenLinksExist()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var query = new GetAdminShopLinksQuery();

        var links = new List<ShopLink>
        {
            new ShopLink { Id = Guid.NewGuid(), Title = "Link 1", Url = "https://link1.com", Category = "CategoryA", IsActive = true, CreatedAt = DateTime.UtcNow },
            new ShopLink { Id = Guid.NewGuid(), Title = "Link 2", Url = "https://link2.com", Category = "CategoryB", IsActive = false, CreatedAt = DateTime.UtcNow }
        };

        _shopLinkRepositoryMock.Setup(x => x.GetAllAsync(cancellationToken))
            .ReturnsAsync(links);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _shopLinkRepositoryMock.Verify(x => x.GetAllAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyList_WhenNoLinksExist()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var query = new GetAdminShopLinksQuery();

        _shopLinkRepositoryMock.Setup(x => x.GetAllAsync(cancellationToken))
            .ReturnsAsync(new List<ShopLink>());

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
