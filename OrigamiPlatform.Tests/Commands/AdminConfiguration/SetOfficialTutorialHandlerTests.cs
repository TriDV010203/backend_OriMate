using Moq;
using OrigamiPlatform.Application.Commands.AdminConfiguration;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.AdminConfiguration;

public class SetOfficialTutorialHandlerTests
{
    private readonly Mock<ITutorialRepository> _tutorialRepoMock = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepoMock = new();

    private SetOfficialTutorialHandler CreateHandler()
        => new(_tutorialRepoMock.Object, _auditLogRepoMock.Object);

    [Fact]
    public async Task HandleAsync_ValidRequest_UpdatesIsOfficialAndLogsAudit()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var tutorialId = Guid.NewGuid();
        var isOfficial = true;
        var command = new SetOfficialTutorialCommand(actorId, tutorialId, isOfficial);

        var existingTutorial = new Tutorial
        {
            Id = tutorialId,
            IsOfficial = false
        };

        _tutorialRepoMock.Setup(r => r.GetByIdWithStepsAsync(tutorialId, default))
            .ReturnsAsync(existingTutorial);

        _tutorialRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Tutorial>(), default))
            .Returns(Task.CompletedTask);

        _auditLogRepoMock.Setup(r => r.LogAsync(It.IsAny<AuditLog>(), default))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.True(existingTutorial.IsOfficial);
        Assert.NotEqual(default, existingTutorial.UpdatedAt);

        _tutorialRepoMock.Verify(r => r.UpdateAsync(existingTutorial, default), Times.Once);
        _auditLogRepoMock.Verify(r => r.LogAsync(It.Is<AuditLog>(l => 
            l.ActorId == actorId && 
            l.Action == "SetOfficialTutorial" && 
            l.EntityType == "Tutorial" && 
            l.EntityId == tutorialId.ToString() && 
            l.OldValue == "False" && 
            l.NewValue == "True"), default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TutorialNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var tutorialId = Guid.NewGuid();
        var command = new SetOfficialTutorialCommand(actorId, tutorialId, true);

        _tutorialRepoMock.Setup(r => r.GetByIdWithStepsAsync(tutorialId, default))
            .ReturnsAsync((Tutorial?)null);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command));
        Assert.Equal($"Tutorial {tutorialId} not found.", ex.Message);

        _tutorialRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Tutorial>(), default), Times.Never);
        _auditLogRepoMock.Verify(r => r.LogAsync(It.IsAny<AuditLog>(), default), Times.Never);
    }
}
