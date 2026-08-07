using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.Commands.Users;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrigamiPlatform.Tests.Commands.Users;

public class CompleteOnboardingHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly CompleteOnboardingHandler _handler;

    public CompleteOnboardingHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new CompleteOnboardingHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new CompleteOnboardingCommand(Guid.NewGuid());
        _userRepositoryMock.Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null!);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("User not found.");
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ProfileNull_CreatesProfileAndSetsOnboardingCompleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CompleteOnboardingCommand(userId);
        var user = new User { Id = userId, Profile = null };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        user.Profile.Should().NotBeNull();
        user.Profile!.UserId.Should().Be(userId);
        user.Profile.IsOnboardingCompleted.Should().BeTrue();
        user.Profile.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ProfileExists_UpdatesOnboardingCompleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CompleteOnboardingCommand(userId);
        var existingProfile = new UserProfile 
        { 
            UserId = userId, 
            IsOnboardingCompleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var user = new User { Id = userId, Profile = existingProfile };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        user.Profile.Should().NotBeNull();
        user.Profile!.IsOnboardingCompleted.Should().BeTrue();
        user.Profile.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }
}
