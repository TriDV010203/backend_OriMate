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

public class UpdateProfileHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly UpdateProfileHandler _handler;

    public UpdateProfileHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new UpdateProfileHandler(_userRepositoryMock.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task HandleAsync_DisplayNameEmpty_ThrowsDomainException(string? invalidDisplayName)
    {
        // Arrange
        var command = new UpdateProfileCommand(Guid.NewGuid(), invalidDisplayName!, null, null);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Tên hiển thị (DisplayName) không được để trống.");
        _userRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_BioTooLong_ThrowsDomainException()
    {
        // Arrange
        var command = new UpdateProfileCommand(Guid.NewGuid(), "Valid Name", null, new string('A', 301));

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Tiểu sử (Bio) không được vượt quá 300 ký tự.");
        _userRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new UpdateProfileCommand(Guid.NewGuid(), "Valid Name", null, null);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Không tìm thấy người dùng.");
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ProfileNull_CreatesProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateProfileCommand(userId, "New Name", "http://example.com/avatar.png", "New Bio");
        var user = new User { Id = userId, Profile = null };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        user.Profile.Should().NotBeNull();
        user.Profile!.UserId.Should().Be(userId);
        user.Profile.DisplayName.Should().Be(command.DisplayName);
        user.Profile.AvatarUrl.Should().Be(command.AvatarUrl);
        user.Profile.Bio.Should().Be(command.Bio);
        user.Profile.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ProfileExists_UpdatesProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateProfileCommand(userId, "Updated Name", "http://example.com/avatar2.png", "Updated Bio");
        var existingProfile = new UserProfile 
        { 
            UserId = userId, 
            DisplayName = "Old Name",
            AvatarUrl = "Old Avatar",
            Bio = "Old Bio",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var user = new User { Id = userId, Profile = existingProfile };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        user.Profile.Should().NotBeNull();
        user.Profile!.DisplayName.Should().Be(command.DisplayName);
        user.Profile.AvatarUrl.Should().Be(command.AvatarUrl);
        user.Profile.Bio.Should().Be(command.Bio);
        user.Profile.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }
}
