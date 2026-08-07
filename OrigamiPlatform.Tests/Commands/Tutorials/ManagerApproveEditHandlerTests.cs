using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Tutorials;

public class ManagerApproveEditHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockTutorialRepo;
    private readonly Mock<INotificationService> _mockNotifications;
    private readonly ManagerApproveEditHandler _handler;

    public ManagerApproveEditHandlerTests()
    {
        _mockTutorialRepo = new Mock<ITutorialRepository>();
        _mockNotifications = new Mock<INotificationService>();
        _handler = new ManagerApproveEditHandler(_mockTutorialRepo.Object, _mockNotifications.Object);
    }

    [Fact]
    public async Task HandleAsync_WorkingCopyNotFound_ThrowsNotFoundException()
    {
        var command = new ManagerApproveEditCommand(Guid.NewGuid(), Guid.NewGuid());
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.WorkingCopyId, default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidStatus_ThrowsDomainException()
    {
        var command = new ManagerApproveEditCommand(Guid.NewGuid(), Guid.NewGuid());
        var workingCopy = new Tutorial { Id = command.WorkingCopyId, Status = TutorialStatus.RevisionRequired };
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.WorkingCopyId, default)).ReturnsAsync(workingCopy);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Only working copies pending manager review", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_NotWorkingCopy_ThrowsDomainException()
    {
        var command = new ManagerApproveEditCommand(Guid.NewGuid(), Guid.NewGuid());
        var workingCopy = new Tutorial { Id = command.WorkingCopyId, Status = TutorialStatus.PendingManagerReview, ParentTutorialId = null };
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.WorkingCopyId, default)).ReturnsAsync(workingCopy);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("not a working copy", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_OriginalNotFound_ThrowsNotFoundException()
    {
        var command = new ManagerApproveEditCommand(Guid.NewGuid(), Guid.NewGuid());
        var originalId = Guid.NewGuid();
        var workingCopy = new Tutorial { Id = command.WorkingCopyId, Status = TutorialStatus.PendingManagerReview, ParentTutorialId = originalId };
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.WorkingCopyId, default)).ReturnsAsync(workingCopy);
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(originalId, default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Contains("Original tutorial", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ApprovesEdit()
    {
        var command = new ManagerApproveEditCommand(Guid.NewGuid(), Guid.NewGuid());
        var originalId = Guid.NewGuid();
        var steps = new List<TutorialStep> { new() { StepOrder = 1, Description = "Edited Step", ImageUrl = "img.jpg" } };
        var workingCopy = new Tutorial { Id = command.WorkingCopyId, Status = TutorialStatus.PendingManagerReview, ParentTutorialId = originalId, Title = "Edited Title", Steps = steps };
        var original = new Tutorial { Id = originalId, Title = "Old Title", AuthorId = Guid.NewGuid() };

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.WorkingCopyId, default)).ReturnsAsync(workingCopy);
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(originalId, default)).ReturnsAsync(original);

        await _handler.HandleAsync(command);

        Assert.Equal("Edited Title", original.Title);
        Assert.Equal(TutorialStatus.Merged, workingCopy.Status);

        _mockTutorialRepo.Verify(r => r.UpdateAsync(original, default), Times.Once);
        _mockTutorialRepo.Verify(r => r.UpdateAsync(workingCopy, default), Times.Once);
        _mockTutorialRepo.Verify(r => r.DeleteStepsByTutorialIdAsync(originalId, default), Times.Once);
        _mockTutorialRepo.Verify(r => r.AddStepsAsync(It.Is<List<TutorialStep>>(s => s.Count == 1), default), Times.Once);
        _mockTutorialRepo.Verify(r => r.AddReviewHistoryAsync(It.Is<TutorialReviewHistory>(h => 
            h.TutorialId == originalId && 
            h.Action == "ApproveEdit"), default), Times.Once);
        _mockNotifications.Verify(n => n.NotifyUserAsync(original.AuthorId, NotificationType.TutorialEditPublished, It.IsAny<string>(), It.IsAny<string>(), originalId, default), Times.Once);
    }
}
