using Moq;
using OrigamiPlatform.Application.Commands.AdminConfiguration;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.AdminConfiguration;

public class RemoveBlockedWordHandlerTests
{
    private readonly Mock<IBlockedWordRepository> _blockedWordRepoMock = new();
    private readonly Mock<IBlockedWordService> _blockedWordServiceMock = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepoMock = new();

    private RemoveBlockedWordHandler CreateHandler()
        => new(_blockedWordRepoMock.Object, _blockedWordServiceMock.Object, _auditLogRepoMock.Object);

    [Fact]
    public async Task HandleAsync_ValidRequest_RemovesBlockedWordAndLogsAudit()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var wordId = 1;
        var command = new RemoveBlockedWordCommand(actorId, wordId);

        var existingWord = new BlockedWord
        {
            Id = wordId,
            Word = "badword"
        };

        _blockedWordRepoMock.Setup(r => r.GetByIdAsync(wordId, default))
            .ReturnsAsync(existingWord);

        _blockedWordRepoMock.Setup(r => r.DeleteAsync(wordId, default))
            .Returns(Task.CompletedTask);

        _blockedWordServiceMock.Setup(s => s.ReloadAsync())
            .Returns(Task.CompletedTask);

        _auditLogRepoMock.Setup(r => r.LogAsync(It.IsAny<AuditLog>(), default))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        _blockedWordRepoMock.Verify(r => r.DeleteAsync(wordId, default), Times.Once);
        _blockedWordServiceMock.Verify(s => s.ReloadAsync(), Times.Once);
        
        _auditLogRepoMock.Verify(r => r.LogAsync(It.Is<AuditLog>(l => 
            l.ActorId == actorId && 
            l.Action == "RemoveBlockedWord" && 
            l.EntityType == "BlockedWord" && 
            l.EntityId == wordId.ToString() && 
            l.OldValue == "badword" && 
            l.NewValue == null), default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WordNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var wordId = 999;
        var command = new RemoveBlockedWordCommand(actorId, wordId);

        _blockedWordRepoMock.Setup(r => r.GetByIdAsync(wordId, default))
            .ReturnsAsync((BlockedWord?)null);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command));
        Assert.Equal($"Blocked word {wordId} not found.", ex.Message);

        _blockedWordRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), default), Times.Never);
        _blockedWordServiceMock.Verify(s => s.ReloadAsync(), Times.Never);
        _auditLogRepoMock.Verify(r => r.LogAsync(It.IsAny<AuditLog>(), default), Times.Never);
    }
}
