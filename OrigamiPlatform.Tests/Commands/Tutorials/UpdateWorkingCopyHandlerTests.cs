using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Features.Tutorials.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Tutorials;

public class UpdateWorkingCopyHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockTutorialRepo;
    private readonly Mock<IBlockedWordService> _mockBlockedWords;
    private readonly UpdateWorkingCopyHandler _handler;

    public UpdateWorkingCopyHandlerTests()
    {
        _mockTutorialRepo = new Mock<ITutorialRepository>();
        _mockBlockedWords = new Mock<IBlockedWordService>();
        _handler = new UpdateWorkingCopyHandler(_mockTutorialRepo.Object, _mockBlockedWords.Object);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new UpdateWorkingCopyCommand(Guid.NewGuid(), Guid.NewGuid(), req);

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.WorkingCopyId, default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidStatus_ThrowsDomainException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new UpdateWorkingCopyCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var workingCopy = new Tutorial { Id = command.WorkingCopyId, AuthorId = command.AuthorId, ParentTutorialId = Guid.NewGuid(), Status = TutorialStatus.PendingManagerReview };

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.WorkingCopyId, default)).ReturnsAsync(workingCopy);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("edit or revision state", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_AuthorMismatch_ThrowsForbiddenException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new UpdateWorkingCopyCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var workingCopy = new Tutorial { Id = command.WorkingCopyId, AuthorId = Guid.NewGuid(), Status = TutorialStatus.EditPendingReview };

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.WorkingCopyId, default)).ReturnsAsync(workingCopy);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _handler.HandleAsync(command));
        Assert.Contains("not the author", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_UpdatesWorkingCopy()
    {
        var steps = new List<CreateTutorialStepRequest> { new(1, "Step 1", null) };
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", steps);
        var command = new UpdateWorkingCopyCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var workingCopy = new Tutorial { Id = command.WorkingCopyId, AuthorId = command.AuthorId, ParentTutorialId = Guid.NewGuid(), Status = TutorialStatus.EditPendingReview, Steps = new List<TutorialStep>() };

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.WorkingCopyId, default)).ReturnsAsync(workingCopy);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCategoryAsync(req.CategoryId, default)).ReturnsAsync(new Category());

        var result = await _handler.HandleAsync(command);

        Assert.NotNull(result);
        Assert.Equal("Valid Title", result.Title);

        _mockTutorialRepo.Verify(r => r.DeleteStepsByTutorialIdAsync(command.WorkingCopyId, default), Times.Once);
        _mockTutorialRepo.Verify(r => r.UpdateAsync(workingCopy, default), Times.Once);
        _mockTutorialRepo.Verify(r => r.AddStepsAsync(It.Is<List<TutorialStep>>(s => s.Count == 1), default), Times.Once);
    }
}
