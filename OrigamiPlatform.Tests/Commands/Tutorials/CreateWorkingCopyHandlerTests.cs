using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Tutorials;

public class CreateWorkingCopyHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockTutorialRepo;
    private readonly CreateWorkingCopyHandler _handler;

    public CreateWorkingCopyHandlerTests()
    {
        _mockTutorialRepo = new Mock<ITutorialRepository>();
        _handler = new CreateWorkingCopyHandler(_mockTutorialRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        var command = new CreateWorkingCopyCommand(Guid.NewGuid(), Guid.NewGuid());
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_AuthorMismatch_ThrowsForbiddenException()
    {
        var command = new CreateWorkingCopyCommand(Guid.NewGuid(), Guid.NewGuid());
        var tutorial = new Tutorial { Id = command.TutorialId, AuthorId = Guid.NewGuid() };
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _handler.HandleAsync(command));
        Assert.Contains("not the author", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_NotPublished_ThrowsDomainException()
    {
        var command = new CreateWorkingCopyCommand(Guid.NewGuid(), Guid.NewGuid());
        var tutorial = new Tutorial { Id = command.TutorialId, AuthorId = command.AuthorId, Status = TutorialStatus.Draft };
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Only published tutorials can be edited", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_WorkingCopyAlreadyExists_ThrowsDomainException()
    {
        var command = new CreateWorkingCopyCommand(Guid.NewGuid(), Guid.NewGuid());
        var tutorial = new Tutorial { Id = command.TutorialId, AuthorId = command.AuthorId, Status = TutorialStatus.Published };
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockTutorialRepo.Setup(r => r.GetWorkingCopyByParentIdAsync(command.TutorialId, default)).ReturnsAsync(new Tutorial());

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("edit is already in progress", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_CreatesWorkingCopyWithUniqueSlug()
    {
        var command = new CreateWorkingCopyCommand(Guid.NewGuid(), Guid.NewGuid());
        var step = new TutorialStep { Id = Guid.NewGuid(), StepOrder = 1, Description = "Step 1" };
        var tutorial = new Tutorial { Id = command.TutorialId, AuthorId = command.AuthorId, Status = TutorialStatus.Published, Slug = "my-tutorial", Steps = new List<TutorialStep> { step } };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockTutorialRepo.Setup(r => r.GetWorkingCopyByParentIdAsync(command.TutorialId, default)).ReturnsAsync((Tutorial?)null);
        
        _mockTutorialRepo.SetupSequence(r => r.SlugExistsAsync("my-tutorial-edit", default))
            .ReturnsAsync(true)
            .ReturnsAsync(false); // second try 'my-tutorial-edit-2'

        var result = await _handler.HandleAsync(command);

        Assert.NotNull(result);
        Assert.Equal(TutorialStatus.EditPendingReview.ToString(), result.Status);

        _mockTutorialRepo.Verify(r => r.AddAsync(It.Is<Tutorial>(t => 
            t.ParentTutorialId == tutorial.Id && 
            t.Slug == "my-tutorial-edit-2" &&
            t.Steps.Count == 1), default), Times.Once);
    }
}
