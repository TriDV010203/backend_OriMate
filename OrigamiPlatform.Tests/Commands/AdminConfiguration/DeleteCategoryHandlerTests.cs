using Moq;
using OrigamiPlatform.Application.Commands.AdminConfiguration;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.AdminConfiguration;

public class DeleteCategoryHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepoMock = new();

    private DeleteCategoryHandler CreateHandler()
        => new(_categoryRepoMock.Object, _auditLogRepoMock.Object);

    [Fact]
    public async Task HandleAsync_ValidRequest_DeletesCategoryAndLogsAudit()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var categoryId = 1;
        var command = new DeleteCategoryCommand(actorId, categoryId);

        var existingCategory = new Category
        {
            Id = categoryId,
            Name = "Origami Animals",
            IsActive = true,
            IsDeleted = false
        };

        _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId, default))
            .ReturnsAsync(existingCategory);

        _categoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Category>(), default))
            .Returns(Task.CompletedTask);

        _auditLogRepoMock.Setup(r => r.LogAsync(It.IsAny<AuditLog>(), default))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.True(existingCategory.IsDeleted);
        Assert.False(existingCategory.IsActive);
        Assert.NotEqual(default, existingCategory.UpdatedAt);

        _categoryRepoMock.Verify(r => r.UpdateAsync(existingCategory, default), Times.Once);
        _auditLogRepoMock.Verify(r => r.LogAsync(It.Is<AuditLog>(l => 
            l.ActorId == actorId && 
            l.Action == "DeleteCategory" && 
            l.EntityType == "Category" && 
            l.EntityId == categoryId.ToString() && 
            l.OldValue == "Origami Animals" && 
            l.NewValue == null), default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_CategoryNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var categoryId = 999;
        var command = new DeleteCategoryCommand(actorId, categoryId);

        _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId, default))
            .ReturnsAsync((Category?)null);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command));
        Assert.Equal($"Category {categoryId} not found.", ex.Message);

        _categoryRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Category>(), default), Times.Never);
        _auditLogRepoMock.Verify(r => r.LogAsync(It.IsAny<AuditLog>(), default), Times.Never);
    }
}
