using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Tutorials;

public class RemoveVariantHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockTutorialRepo;
    private readonly Mock<ITutorialVariantRepository> _mockVariantRepo;
    private readonly RemoveVariantHandler _handler;

    public RemoveVariantHandlerTests()
    {
        _mockTutorialRepo = new Mock<ITutorialRepository>();
        _mockVariantRepo = new Mock<ITutorialVariantRepository>();
        _handler = new RemoveVariantHandler(_mockTutorialRepo.Object, _mockVariantRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_LinkNotFound_ThrowsNotFoundException()
    {
        var command = new RemoveVariantCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _mockVariantRepo.Setup(r => r.GetByPairAsync(command.ParentTutorialId, command.VariantTutorialId, default)).ReturnsAsync((TutorialVariant?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Contains("Variant link not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ParentNotFound_ThrowsNotFoundException()
    {
        var command = new RemoveVariantCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _mockVariantRepo.Setup(r => r.GetByPairAsync(command.ParentTutorialId, command.VariantTutorialId, default)).ReturnsAsync(new TutorialVariant());
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.ParentTutorialId, default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_AuthorMismatch_ThrowsForbiddenException()
    {
        var command = new RemoveVariantCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _mockVariantRepo.Setup(r => r.GetByPairAsync(command.ParentTutorialId, command.VariantTutorialId, default)).ReturnsAsync(new TutorialVariant());
        var parent = new Tutorial { Id = command.ParentTutorialId, AuthorId = Guid.NewGuid() };
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.ParentTutorialId, default)).ReturnsAsync(parent);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _handler.HandleAsync(command));
        Assert.Contains("not the author", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_DeletesVariant()
    {
        var command = new RemoveVariantCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var variant = new TutorialVariant();
        _mockVariantRepo.Setup(r => r.GetByPairAsync(command.ParentTutorialId, command.VariantTutorialId, default)).ReturnsAsync(variant);
        var parent = new Tutorial { Id = command.ParentTutorialId, AuthorId = command.RequesterId };
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.ParentTutorialId, default)).ReturnsAsync(parent);

        await _handler.HandleAsync(command);

        _mockVariantRepo.Verify(r => r.DeleteAsync(variant, default), Times.Once);
    }
}
