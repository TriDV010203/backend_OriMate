using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Features.Tutorials.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Tutorials;

public class ManagerRejectEditHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockTutorialRepo;
    private readonly Mock<INotificationService> _mockNotifications;
    private readonly ManagerRejectEditHandler _handler;

    public ManagerRejectEditHandlerTests()
    {
        _mockTutorialRepo = new Mock<ITutorialRepository>();
        _mockNotifications = new Mock<INotificationService>();
        _handler = new ManagerRejectEditHandler(_mockTutorialRepo.Object, _mockNotifications.Object);
    }

    [Fact]
    public async Task HandleAsync_ShortReason_ThrowsDomainException()
    {
        var req = new ManagerRejectRequest("Short");
        var command = new ManagerRejectEditCommand(Guid.NewGuid(), Guid.NewGuid(), req);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("reason must be at least 10 characters", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        var req = new ManagerRejectRequest("This is a valid rejection reason.");
        var command = new ManagerRejectEditCommand(Guid.NewGuid(), Guid.NewGuid(), req);

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.WorkingCopyId, default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidStatus_ThrowsDomainException()
    {
        var req = new ManagerRejectRequest("This is a valid rejection reason.");
        var command = new ManagerRejectEditCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var workingCopy = new Tutorial { Id = command.WorkingCopyId, Status = TutorialStatus.RevisionRequired };

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.WorkingCopyId, default)).ReturnsAsync(workingCopy);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Only working copies pending manager review", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_RejectsEdit()
    {
        var req = new ManagerRejectRequest("This is a valid rejection reason.");
        var command = new ManagerRejectEditCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var workingCopy = new Tutorial { Id = command.WorkingCopyId, Status = TutorialStatus.PendingManagerReview, AuthorId = Guid.NewGuid() };

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.WorkingCopyId, default)).ReturnsAsync(workingCopy);

        await _handler.HandleAsync(command);

        Assert.Equal(TutorialStatus.RevisionRequired, workingCopy.Status);
        _mockTutorialRepo.Verify(r => r.UpdateAsync(workingCopy, default), Times.Once);
        _mockTutorialRepo.Verify(r => r.AddReviewHistoryAsync(It.Is<TutorialReviewHistory>(h => 
            h.Action == "RejectEdit" && 
            h.ToStatus == TutorialStatus.RevisionRequired &&
            h.Reason == req.Reason), default), Times.Once);
        _mockNotifications.Verify(n => n.NotifyUserAsync(workingCopy.AuthorId, NotificationType.TutorialEditRejected, It.IsAny<string>(), It.IsAny<string>(), workingCopy.Id, default), Times.Once);
    }
}
