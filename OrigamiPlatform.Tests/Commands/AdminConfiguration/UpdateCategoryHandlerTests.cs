using System.Text.Json;
using Moq;
using OrigamiPlatform.Application.Commands.AdminConfiguration;
using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.AdminConfiguration;

public class UpdateCategoryHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepoMock = new();

    private UpdateCategoryHandler CreateHandler()
        => new(_categoryRepoMock.Object, _auditLogRepoMock.Object);

    [Fact]
    public async Task HandleAsync_ValidRequest_UpdatesCategoryAndLogsAudit()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var categoryId = 1;
        var request = new UpdateCategoryRequest("New Name", false);
        var command = new UpdateCategoryCommand(actorId, categoryId, request);

        var existingCategory = new Category
        {
            Id = categoryId,
            Name = "Old Name",
            IsActive = true
        };

        _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId, default))
            .ReturnsAsync(existingCategory);

        _categoryRepoMock.Setup(r => r.ExistsByNameAsync("New Name", categoryId, default))
            .ReturnsAsync(false);

        _categoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Category>(), default))
            .Returns(Task.CompletedTask);

        _auditLogRepoMock.Setup(r => r.LogAsync(It.IsAny<AuditLog>(), default))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.Equal("New Name", existingCategory.Name);
        Assert.False(existingCategory.IsActive);
        Assert.NotEqual(default, existingCategory.UpdatedAt);
        
        Assert.Equal(categoryId, result.Id);
        Assert.Equal("New Name", result.Name);
        Assert.False(result.IsActive);

        var oldState = JsonSerializer.Serialize(new { Name = "Old Name", IsActive = true });
        var newState = JsonSerializer.Serialize(new { Name = "New Name", IsActive = false });

        _categoryRepoMock.Verify(r => r.UpdateAsync(existingCategory, default), Times.Once);
        _auditLogRepoMock.Verify(r => r.LogAsync(It.Is<AuditLog>(l => 
            l.ActorId == actorId && 
            l.Action == "UpdateCategory" && 
            l.EntityType == "Category" && 
            l.EntityId == categoryId.ToString() && 
            l.OldValue == oldState && 
            l.NewValue == newState), default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NameExists_ThrowsConflictException()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var categoryId = 1;
        var request = new UpdateCategoryRequest("Existing Name", true);
        var command = new UpdateCategoryCommand(actorId, categoryId, request);

        var existingCategory = new Category
        {
            Id = categoryId,
            Name = "Old Name",
            IsActive = true
        };

        _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId, default))
            .ReturnsAsync(existingCategory);

        _categoryRepoMock.Setup(r => r.ExistsByNameAsync("Existing Name", categoryId, default))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(command));
        Assert.Equal("Category name already exists.", ex.Message);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("This category name is definitely way too long to be accepted because it exceeds fifty characters")]
    public async Task HandleAsync_InvalidNameLength_ThrowsDomainException(string invalidName)
    {
        // Arrange
        var command = new UpdateCategoryCommand(Guid.NewGuid(), 1, new UpdateCategoryRequest(invalidName, true));
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(command));
        Assert.Equal("Category name must be between 2 and 50 characters.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_CategoryNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var categoryId = 999;
        var command = new UpdateCategoryCommand(actorId, categoryId, new UpdateCategoryRequest("Name", true));

        _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId, default))
            .ReturnsAsync((Category?)null);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command));
        Assert.Equal($"Category {categoryId} not found.", ex.Message);
    }
}
