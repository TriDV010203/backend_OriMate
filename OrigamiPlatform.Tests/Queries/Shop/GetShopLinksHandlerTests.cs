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

public class GetShopLinksHandlerTests
{
    private readonly Mock<IShopLinkRepository> _shopLinkRepositoryMock;
    private readonly GetShopLinksHandler _handler;

    public GetShopLinksHandlerTests()
    {
        _shopLinkRepositoryMock = new Mock<IShopLinkRepository>();
        _handler = new GetShopLinksHandler(_shopLinkRepositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnActiveShopLinks_WhenLinksExist()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var query = new GetShopLinksQuery();

        var links = new List<ShopLink>
        {
            new ShopLink { Id = Guid.NewGuid(), Title = "Link 1", Url = "https://link1.com", Category = "CategoryA", IsActive = true, CreatedAt = DateTime.UtcNow },
            new ShopLink { Id = Guid.NewGuid(), Title = "Link 2", Url = "https://link2.com", Category = "CategoryB", IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        _shopLinkRepositoryMock.Setup(x => x.GetActiveAsync(cancellationToken))
            .ReturnsAsync(links);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _shopLinkRepositoryMock.Verify(x => x.GetActiveAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyList_WhenNoActiveLinksExist()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var query = new GetShopLinksQuery();

        _shopLinkRepositoryMock.Setup(x => x.GetActiveAsync(cancellationToken))
            .ReturnsAsync(new List<ShopLink>());

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
