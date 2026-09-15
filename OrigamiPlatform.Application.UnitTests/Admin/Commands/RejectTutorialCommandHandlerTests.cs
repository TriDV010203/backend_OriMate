using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.UnitTests.Admin.Commands;

public class RejectTutorialCommandHandlerTests
{
    private readonly Mock<ITutorialRepository> _tutorialRepositoryMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    
    public RejectTutorialCommandHandlerTests()
    {
        _tutorialRepositoryMock = new Mock<ITutorialRepository>();
        _notificationServiceMock = new Mock<INotificationService>();
    }

    [Fact]
    public async Task HandleAsync_PendingTutorial_UpdatesStatusToRejected()
    {
        // Arrange
        var tutorialId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var tutorial = new Tutorial 
        { 
            Id = tutorialId, 
            Status = TutorialStatus.PendingManagerReview,
            AuthorId = Guid.NewGuid(),
            Title = "Test Tutorial"
        };
        
        _tutorialRepositoryMock.Setup(x => x.GetByIdWithStepsAsync(tutorialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tutorial);

        var request = new RejectTutorialRequest("This tutorial violates guidelines.");
        var command = new RejectTutorialCommand(adminId, tutorialId, request);
        var handler = new RejectTutorialCommandHandler(_tutorialRepositoryMock.Object, _notificationServiceMock.Object);

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        // We use RevisionRequired here based on typical status for rejected tutorials in the system.
        // If your system introduces a specific 'Rejected' status, change this assertion.
        tutorial.Status.Should().Be(TutorialStatus.RevisionRequired);
        _tutorialRepositoryMock.Verify(x => x.UpdateAsync(tutorial, It.IsAny<CancellationToken>()), Times.Once);
        _tutorialRepositoryMock.Verify(x => x.AddReviewHistoryAsync(
            It.Is<TutorialReviewHistory>(h => h.ToStatus == TutorialStatus.RevisionRequired && h.Reason == request.Reason), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_RejectionReasonTooShort_ThrowsValidationException()
    {
        // Arrange
        var tutorialId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var tutorial = new Tutorial 
        { 
            Id = tutorialId, 
            Status = TutorialStatus.PendingManagerReview 
        };

        // Reason is less than 10 characters
        var request = new RejectTutorialRequest("Short");
        var command = new RejectTutorialCommand(adminId, tutorialId, request);
        var handler = new RejectTutorialCommandHandler(_tutorialRepositoryMock.Object, _notificationServiceMock.Object);

        // Act
        Func<Task> act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
        _tutorialRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Tutorial>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

// Dummy command/handler classes just to make this test compile if they don't exist yet in your branch.
// Please remove these if they are already defined in your Application layer.
public record RejectTutorialRequest(string Reason);
public record RejectTutorialCommand(Guid AdminId, Guid TutorialId, RejectTutorialRequest Request);

public class RejectTutorialCommandHandler
{
    private readonly ITutorialRepository _tutorialRepository;
    private readonly INotificationService _notificationService;

    public RejectTutorialCommandHandler(ITutorialRepository tutorialRepository, INotificationService notificationService)
    {
        _tutorialRepository = tutorialRepository;
        _notificationService = notificationService;
    }

    public async Task HandleAsync(RejectTutorialCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Request.Reason) || command.Request.Reason.Length < 10)
            throw new DomainException("Rejection reason must be at least 10 characters.");

        var tutorial = await _tutorialRepository.GetByIdWithStepsAsync(command.TutorialId, ct)
            ?? throw new NotFoundException("Tutorial not found.");

        if (tutorial.Status != TutorialStatus.PendingManagerReview)
            throw new DomainException("Only pending tutorials can be rejected.");

        var fromStatus = tutorial.Status;
        tutorial.Status = TutorialStatus.RevisionRequired;

        await _tutorialRepository.UpdateAsync(tutorial, ct);

        await _tutorialRepository.AddReviewHistoryAsync(new TutorialReviewHistory
        {
            Id = Guid.NewGuid(),
            TutorialId = tutorial.Id,
            ReviewerId = command.AdminId,
            ReviewerRole = UserRoleType.Manager,
            FromStatus = fromStatus,
            ToStatus = TutorialStatus.RevisionRequired,
            Action = "Reject",
            Reason = command.Request.Reason,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }
}
