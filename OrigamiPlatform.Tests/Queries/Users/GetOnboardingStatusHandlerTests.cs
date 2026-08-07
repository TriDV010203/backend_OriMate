using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.Queries.Users;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;
using Xunit;

namespace OrigamiPlatform.Tests.Queries.Users;

public class GetOnboardingStatusHandlerTests
{
    private readonly Mock<IUserRepository> _usersMock;
    private readonly GetOnboardingStatusHandler _handler;

    public GetOnboardingStatusHandlerTests()
    {
        _usersMock = new Mock<IUserRepository>();
        _handler = new GetOnboardingStatusHandler(_usersMock.Object);
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var query = new GetOnboardingStatusQuery(Guid.NewGuid());
        _usersMock.Setup(x => x.GetByIdAsync(query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await FluentActions.Invoking(() => _handler.HandleAsync(query))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("User not found.");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_UserExists_ReturnsStatus(bool isCompleted)
    {
        // Arrange
        var query = new GetOnboardingStatusQuery(Guid.NewGuid());
        var user = new User
        {
            Id = query.UserId,
            Profile = new UserProfile
            {
                IsOnboardingCompleted = isCompleted
            }
        };

        _usersMock.Setup(x => x.GetByIdAsync(query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.IsOnboardingCompleted.Should().Be(isCompleted);
    }

    [Fact]
    public async Task HandleAsync_UserProfileNull_ReturnsFalse()
    {
        // Arrange
        var query = new GetOnboardingStatusQuery(Guid.NewGuid());
        var user = new User
        {
            Id = query.UserId,
            Profile = null
        };

        _usersMock.Setup(x => x.GetByIdAsync(query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.IsOnboardingCompleted.Should().BeFalse();
    }
}
