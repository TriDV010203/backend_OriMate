using Moq;
using OrigamiPlatform.Application.Commands.AdminConfiguration;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.Tests.Commands.AdminConfiguration;

public class ExplicitActivateUserHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IAuditLogRepository> _auditLogMock;
    private readonly Mock<INotificationService> _notificationsMock;
    private readonly ActivateUserHandler _handler;

    public ExplicitActivateUserHandlerTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _auditLogMock = new Mock<IAuditLogRepository>();
        _notificationsMock = new Mock<INotificationService>();
        _handler = new ActivateUserHandler(_userRepoMock.Object, _auditLogMock.Object, _notificationsMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidSuspendedUser_ActivatesUserAndLogsAudit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var command = new ActivateUserCommand(actorId, userId);

        var suspendedUser = new User
        {
            Id = userId,
            Status = AccountStatus.Suspended
        };

        _userRepoMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendedUser);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        Assert.Equal(AccountStatus.Active, suspendedUser.Status);
        
        _userRepoMock.Verify(x => x.UpdateAsync(suspendedUser, It.IsAny<CancellationToken>()), Times.Once);
        
        _auditLogMock.Verify(x => x.LogAsync(It.Is<AuditLog>(l => 
            l.ActorId == actorId && 
            l.Action == "ActivateAccount" && 
            l.EntityId == userId.ToString()), It.IsAny<CancellationToken>()), Times.Once);
            
        _notificationsMock.Verify(x => x.NotifyUserAsync(
            userId, 
            NotificationType.AccountActivated, 
            It.IsAny<string>(), 
            "User", 
            userId, 
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
