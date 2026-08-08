using Moq;
using OrigamiPlatform.Application.Commands.AdminConfiguration;
using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using Xunit;

namespace OrigamiPlatform.Tests.Commands.AdminConfiguration;

public class ExplicitCreateCategoryHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepoMock;
    private readonly Mock<IAuditLogRepository> _auditLogMock;
    private readonly CreateCategoryHandler _handler;

    public ExplicitCreateCategoryHandlerTests()
    {
        _categoryRepoMock = new Mock<ICategoryRepository>();
        _auditLogMock = new Mock<IAuditLogRepository>();
        _handler = new CreateCategoryHandler(_categoryRepoMock.Object, _auditLogMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_CreatesCategoryAndLogsAudit()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var request = new CreateCategoryRequest("New Category");
        var command = new CreateCategoryCommand(actorId, request);

        _categoryRepoMock
            .Setup(x => x.ExistsByNameAsync(request.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _categoryRepoMock
            .Setup(x => x.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category c, CancellationToken ct) => 
            {
                c.Id = 1;
                return c;
            });

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.True(result.IsActive);

        _categoryRepoMock.Verify(x => x.AddAsync(It.Is<Category>(c => c.Name == request.Name), It.IsAny<CancellationToken>()), Times.Once);
        _auditLogMock.Verify(x => x.LogAsync(It.Is<AuditLog>(l => l.ActorId == actorId && l.Action == "CreateCategory"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
