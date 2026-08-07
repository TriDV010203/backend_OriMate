using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Tutorials;

public class SubmitEditHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockTutorialRepo;
    private readonly Mock<INotificationService> _mockNotifications;
    private readonly SubmitEditHandler _handler;

    public SubmitEditHandlerTests()
    {
        _mockTutorialRepo = new Mock<ITutorialRepository>();
        _mockNotifications = new Mock<INotificationService>();
        _handler = new SubmitEditHandler(_mockTutorialRepo.Object, _mockNotifications.Object);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        var command = new SubmitEditCommand(Guid.NewGuid(), Guid.NewGuid());
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.WorkingCopyId, default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_AuthorMismatch_ThrowsForbiddenException()
    {
        var command = new SubmitEditCommand(Guid.NewGuid(), Guid.NewGuid());
        var workingCopy = new Tutorial { Id = command.WorkingCopyId, AuthorId = Guid.NewGuid(), Status = TutorialStatus.RevisionRequired };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.WorkingCopyId, default)).ReturnsAsync(workingCopy);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _handler.HandleAsync(command));
        Assert.Contains("not the author", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_NotWorkingCopy_ThrowsDomainException()
    {
        var command = new SubmitEditCommand(Guid.NewGuid(), Guid.NewGuid());
        var workingCopy = new Tutorial { Id = command.WorkingCopyId, AuthorId = command.AuthorId, Status = TutorialStatus.Draft };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.WorkingCopyId, default)).ReturnsAsync(workingCopy);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("edit or revision state", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_NotEnoughSteps_ThrowsDomainException()
    {
        var command = new SubmitEditCommand(Guid.NewGuid(), Guid.NewGuid());
        var workingCopy = new Tutorial { Id = command.WorkingCopyId, AuthorId = command.AuthorId, ParentTutorialId = Guid.NewGuid(), Status = TutorialStatus.RevisionRequired, Title = "Valid Title", Description = "Valid description spanning 20 chars!", CoverImageUrl = "cover.jpg", Steps = new List<TutorialStep>() };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.WorkingCopyId, default)).ReturnsAsync(workingCopy);
        _mockTutorialRepo.Setup(r => r.GetActiveCategoryAsync(It.IsAny<int>(), default)).ReturnsAsync(new Category());

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("between 3 and 30 steps", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_SubmitsEditAndNotifiesManager()
    {
        var command = new SubmitEditCommand(Guid.NewGuid(), Guid.NewGuid());
        var steps = new List<TutorialStep> 
        {
            new() { StepOrder = 1, Description = "Step 1", ImageUrl = "img.jpg" },
            new() { StepOrder = 2, Description = "Step 2", ImageUrl = "img2.jpg" },
            new() { StepOrder = 3, Description = "Step 3", ImageUrl = "img3.jpg" }
        };
        var workingCopy = new Tutorial 
        {
            Id = command.WorkingCopyId, 
            AuthorId = command.AuthorId, 
            ParentTutorialId = Guid.NewGuid(), 
            Status = TutorialStatus.EditPendingReview, 
            Title = "Valid Title", 
            Description = "Valid description spanning 20 chars!", 
            CoverImageUrl = "cover.jpg", 
            Steps = steps,
            CategoryId = 1
        };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.WorkingCopyId, default)).ReturnsAsync(workingCopy);
        _mockTutorialRepo.Setup(r => r.GetActiveCategoryAsync(1, default)).ReturnsAsync(new Category());

        await _handler.HandleAsync(command);

        Assert.Equal(TutorialStatus.PendingManagerReview, workingCopy.Status);
        _mockTutorialRepo.Verify(r => r.UpdateAsync(workingCopy, default), Times.Once);
        _mockTutorialRepo.Verify(r => r.AddReviewHistoryAsync(It.Is<TutorialReviewHistory>(h => 
            h.Action == "SubmitEdit" && 
            h.ToStatus == TutorialStatus.PendingManagerReview), default), Times.Once);
        _mockNotifications.Verify(n => n.NotifyUsersWithRoleAsync(UserRoleType.Manager, NotificationType.TutorialReadyForManagerApproval, It.IsAny<string>(), It.IsAny<string>(), workingCopy.Id, default), Times.Once);
    }
}
