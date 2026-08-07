using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Tutorials;

public class AddVariantHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockTutorialRepo;
    private readonly Mock<ITutorialVariantRepository> _mockVariantRepo;
    private readonly AddVariantHandler _handler;

    public AddVariantHandlerTests()
    {
        _mockTutorialRepo = new Mock<ITutorialRepository>();
        _mockVariantRepo = new Mock<ITutorialVariantRepository>();
        _handler = new AddVariantHandler(_mockTutorialRepo.Object, _mockVariantRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_SameIds_ThrowsDomainException()
    {
        var id = Guid.NewGuid();
        var command = new AddVariantCommand(Guid.NewGuid(), id, id, 1);
        var parent = new Tutorial { Id = id, AuthorId = command.RequesterId };
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(id, default)).ReturnsAsync(parent);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Cannot link tutorial to itself", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ParentNotFound_ThrowsNotFoundException()
    {
        var command = new AddVariantCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.ParentTutorialId, default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_VariantNotFound_ThrowsNotFoundException()
    {
        var command = new AddVariantCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        var parent = new Tutorial { Id = command.ParentTutorialId, AuthorId = command.RequesterId };

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.ParentTutorialId, default)).ReturnsAsync(parent);
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.VariantTutorialId, default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_LinkAlreadyExists_ThrowsDomainException()
    {
        var command = new AddVariantCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        var parent = new Tutorial { Id = command.ParentTutorialId, AuthorId = command.RequesterId };
        var variant = new Tutorial { Id = command.VariantTutorialId, AuthorId = command.RequesterId };

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.ParentTutorialId, default)).ReturnsAsync(parent);
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.VariantTutorialId, default)).ReturnsAsync(variant);
        _mockVariantRepo.Setup(r => r.GetByPairAsync(command.ParentTutorialId, command.VariantTutorialId, default)).ReturnsAsync(new TutorialVariant());

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("already linked", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_CreatesVariantLink()
    {
        var command = new AddVariantCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        var parent = new Tutorial { Id = command.ParentTutorialId, AuthorId = command.RequesterId };
        var variant = new Tutorial { Id = command.VariantTutorialId };

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.ParentTutorialId, default)).ReturnsAsync(parent);
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.VariantTutorialId, default)).ReturnsAsync(variant);
        _mockVariantRepo.Setup(r => r.GetByPairAsync(command.ParentTutorialId, command.VariantTutorialId, default)).ReturnsAsync((TutorialVariant?)null);

        await _handler.HandleAsync(command);

        _mockVariantRepo.Verify(r => r.AddAsync(It.Is<TutorialVariant>(v =>
            v.ParentTutorialId == command.ParentTutorialId &&
            v.VariantTutorialId == command.VariantTutorialId &&
            v.DifficultyDelta == 1), default), Times.Once);
    }
}
