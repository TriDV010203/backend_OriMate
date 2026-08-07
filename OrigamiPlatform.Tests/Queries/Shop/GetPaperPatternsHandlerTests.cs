using System;
using System.Collections.Generic;
using System.Linq;
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

public class GetPaperPatternsHandlerTests
{
    private readonly Mock<IPaperPatternRepository> _patternsMock;
    private readonly Mock<IUserPaperPatternRepository> _userPatternsMock;
    private readonly GetPaperPatternsHandler _handler;

    public GetPaperPatternsHandlerTests()
    {
        _patternsMock = new Mock<IPaperPatternRepository>();
        _userPatternsMock = new Mock<IUserPaperPatternRepository>();
        _handler = new GetPaperPatternsHandler(_patternsMock.Object, _userPatternsMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnPatterns_WithCorrectOwnership()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var userId = Guid.NewGuid();
        var query = new GetPaperPatternsQuery(userId);

        var p1Id = Guid.NewGuid();
        var p2Id = Guid.NewGuid();
        var p3Id = Guid.NewGuid();

        var patterns = new List<PaperPattern>
        {
            new PaperPattern { Id = p1Id, Name = "Pattern 1", PriceInHatGap = 10, IsActive = true, CreatedAt = DateTime.UtcNow },
            new PaperPattern { Id = p2Id, Name = "Pattern 2", PriceInHatGap = 20, IsActive = true, CreatedAt = DateTime.UtcNow },
            new PaperPattern { Id = p3Id, Name = "Pattern 3", PriceInHatGap = 30, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var userOwnedPatterns = new List<UserPaperPattern>
        {
            new UserPaperPattern { UserId = userId, PaperPatternId = p1Id, PurchasedAt = DateTime.UtcNow },
            new UserPaperPattern { UserId = userId, PaperPatternId = p3Id, PurchasedAt = DateTime.UtcNow }
        };

        _patternsMock.Setup(x => x.GetActiveAsync(cancellationToken))
            .ReturnsAsync(patterns);
            
        _userPatternsMock.Setup(x => x.GetByUserIdAsync(query.UserId, cancellationToken))
            .ReturnsAsync(userOwnedPatterns);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        var pattern1 = result.Single(p => p.Id == p1Id);
        pattern1.IsOwned.Should().BeTrue();
        pattern1.Name.Should().Be("Pattern 1");
        pattern1.PriceInHatGap.Should().Be(10);

        var pattern2 = result.Single(p => p.Id == p2Id);
        pattern2.IsOwned.Should().BeFalse();
        
        var pattern3 = result.Single(p => p.Id == p3Id);
        pattern3.IsOwned.Should().BeTrue();

        _patternsMock.Verify(x => x.GetActiveAsync(cancellationToken), Times.Once);
        _userPatternsMock.Verify(x => x.GetByUserIdAsync(query.UserId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAllAsNotOwned_WhenUserHasNoPatterns()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var userId = Guid.NewGuid();
        var query = new GetPaperPatternsQuery(userId);

        var patterns = new List<PaperPattern>
        {
            new PaperPattern { Id = Guid.NewGuid(), Name = "Pattern 1", PriceInHatGap = 10, IsActive = true, CreatedAt = DateTime.UtcNow },
            new PaperPattern { Id = Guid.NewGuid(), Name = "Pattern 2", PriceInHatGap = 20, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        _patternsMock.Setup(x => x.GetActiveAsync(cancellationToken))
            .ReturnsAsync(patterns);
            
        _userPatternsMock.Setup(x => x.GetByUserIdAsync(query.UserId, cancellationToken))
            .ReturnsAsync(new List<UserPaperPattern>());

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(p => !p.IsOwned).Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmpty_WhenNoPatternsExist()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var userId = Guid.NewGuid();
        var query = new GetPaperPatternsQuery(userId);

        _patternsMock.Setup(x => x.GetActiveAsync(cancellationToken))
            .ReturnsAsync(new List<PaperPattern>());
            
        _userPatternsMock.Setup(x => x.GetByUserIdAsync(query.UserId, cancellationToken))
            .ReturnsAsync(new List<UserPaperPattern>());

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
