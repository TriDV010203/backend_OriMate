using Moq;
using OrigamiPlatform.Application.Commands.AdminConfiguration;
using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.AdminConfiguration;

public class SuspendUserHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepoMock = new();
    private readonly Mock<INotificationService> _notificationsMock = new();

    private SuspendUserHandler CreateHandler()
        => new(_userRepoMock.Object, _auditLogRepoMock.Object, _notificationsMock.Object);

    [Fact]
    public async Task HandleAsync_ValidRequest_SuspendsUserAndLogsAuditAndNotifies()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new SuspendUserRequest("Violation of terms");
        var command = new SuspendUserCommand(actorId, userId, request);

        var existingUser = new User
        {
            Id = userId,
            Status = AccountStatus.Active,
            Roles = new List<UserRole> { new UserRole { Role = UserRoleType.User } }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(existingUser);

        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>(), default))
            .Returns(Task.CompletedTask);

        _auditLogRepoMock.Setup(r => r.LogAsync(It.IsAny<AuditLog>(), default))
            .Returns(Task.CompletedTask);

        _notificationsMock.Setup(n => n.NotifyUserAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), default))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.Equal(AccountStatus.Suspended, existingUser.Status);
        Assert.NotEqual(default, existingUser.UpdatedAt);

        _userRepoMock.Verify(r => r.UpdateAsync(existingUser, default), Times.Once);
        _auditLogRepoMock.Verify(r => r.LogAsync(It.Is<AuditLog>(l => 
            l.ActorId == actorId && 
            l.Action == "SuspendAccount" && 
            l.EntityType == "User" && 
            l.EntityId == userId.ToString() && 
            l.OldValue == null && 
            l.NewValue == "Violation of terms"), default), Times.Once);
            
        _notificationsMock.Verify(n => n.NotifyUserAsync(
            userId,
            NotificationType.AccountSuspended,
            "Your account has been suspended: Violation of terms",
            "User",
            userId,
            default), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_EmptyReason_ThrowsDomainException(string? invalidReason)
    {
        // Arrange
        var command = new SuspendUserCommand(Guid.NewGuid(), Guid.NewGuid(), new SuspendUserRequest(invalidReason!));
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(command));
        Assert.Equal("Suspension reason is required.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ShortReason_ThrowsDomainException()
    {
        // Arrange
        var command = new SuspendUserCommand(Guid.NewGuid(), Guid.NewGuid(), new SuspendUserRequest("Short"));
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(command));
        Assert.Equal("Suspension reason must be at least 10 characters.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new SuspendUserCommand(Guid.NewGuid(), userId, new SuspendUserRequest("Valid Reason"));

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command));
        Assert.Equal($"User {userId} not found.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_UserIsAdmin_ThrowsForbiddenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new SuspendUserCommand(Guid.NewGuid(), userId, new SuspendUserRequest("Valid Reason"));

        var existingUser = new User
        {
            Id = userId,
            Roles = new List<UserRole> { new UserRole { Role = UserRoleType.Admin } }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(existingUser);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(command));
        Assert.Equal("Cannot suspend an Admin account.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_UserAlreadySuspended_ThrowsBadRequestException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new SuspendUserCommand(Guid.NewGuid(), userId, new SuspendUserRequest("Valid Reason"));

        var existingUser = new User
        {
            Id = userId,
            Status = AccountStatus.Suspended,
            Roles = new List<UserRole> { new UserRole { Role = UserRoleType.User } }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(existingUser);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => handler.HandleAsync(command));
        Assert.Equal("User account is already suspended.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_UserNotActive_ThrowsBadRequestException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new SuspendUserCommand(Guid.NewGuid(), userId, new SuspendUserRequest("Valid Reason"));

        var existingUser = new User
        {
            Id = userId,
            Status = (AccountStatus)999, // Any status other than Active/Suspended
            Roles = new List<UserRole> { new UserRole { Role = UserRoleType.User } }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(existingUser);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => handler.HandleAsync(command));
        Assert.Equal("Only Active accounts can be suspended.", ex.Message);
    }
}
