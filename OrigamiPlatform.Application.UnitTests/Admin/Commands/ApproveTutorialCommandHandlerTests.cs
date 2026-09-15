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
// Note: Depending on your exact command location, you may need to adjust the using statement for the Commands namespace.

namespace OrigamiPlatform.Application.UnitTests.Admin.Commands;

public class ApproveTutorialCommandHandlerTests
{
    private readonly Mock<ITutorialRepository> _tutorialRepositoryMock;
    // We mock INotificationService if it's typically injected, based on existing context.
    private readonly Mock<INotificationService> _notificationServiceMock;
    
    public ApproveTutorialCommandHandlerTests()
    {
        _tutorialRepositoryMock = new Mock<ITutorialRepository>();
        _notificationServiceMock = new Mock<INotificationService>();
    }

    [Fact]
    public async Task HandleAsync_PendingTutorial_UpdatesStatusToPublished()
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

        // Assume the command looks like this based on common CQRS patterns in the solution
        // If your command is named differently, please adjust accordingly.
        var command = new ApproveTutorialCommand(adminId, tutorialId); 
        var handler = new ApproveTutorialCommandHandler(_tutorialRepositoryMock.Object, _notificationServiceMock.Object);

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        tutorial.Status.Should().Be(TutorialStatus.Published);
        _tutorialRepositoryMock.Verify(x => x.UpdateAsync(tutorial, It.IsAny<CancellationToken>()), Times.Once);
        // Verify review history is added based on BR-17 if applicable
        _tutorialRepositoryMock.Verify(x => x.AddReviewHistoryAsync(It.Is<TutorialReviewHistory>(h => h.ToStatus == TutorialStatus.Published), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TutorialNotPending_ThrowsDomainException()
    {
        // Arrange
        var tutorialId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var tutorial = new Tutorial 
        { 
            Id = tutorialId, 
            Status = TutorialStatus.Draft // Not pending
        };
        
        _tutorialRepositoryMock.Setup(x => x.GetByIdWithStepsAsync(tutorialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tutorial);

        var command = new ApproveTutorialCommand(adminId, tutorialId);
        var handler = new ApproveTutorialCommandHandler(_tutorialRepositoryMock.Object, _notificationServiceMock.Object);

        // Act
        Func<Task> act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
        _tutorialRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Tutorial>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

// Dummy command/handler classes just to make this test compile if they don't exist yet in your branch.
// Please remove these if they are already defined in your Application layer.
public record ApproveTutorialCommand(Guid AdminId, Guid TutorialId);

public class ApproveTutorialCommandHandler
{
    private readonly ITutorialRepository _tutorialRepository;
    private readonly INotificationService _notificationService;

    public ApproveTutorialCommandHandler(ITutorialRepository tutorialRepository, INotificationService notificationService)
    {
        _tutorialRepository = tutorialRepository;
        _notificationService = notificationService;
    }

    public async Task HandleAsync(ApproveTutorialCommand command, CancellationToken ct)
    {
        var tutorial = await _tutorialRepository.GetByIdWithStepsAsync(command.TutorialId, ct)
            ?? throw new NotFoundException($"Tutorial not found.");

        if (tutorial.Status != TutorialStatus.PendingManagerReview)
            throw new DomainException("Tutorial is not pending.");

        var fromStatus = tutorial.Status;
        tutorial.Status = TutorialStatus.Published;

        await _tutorialRepository.UpdateAsync(tutorial, ct);

        await _tutorialRepository.AddReviewHistoryAsync(new TutorialReviewHistory
        {
            Id = Guid.NewGuid(),
            TutorialId = tutorial.Id,
            ReviewerId = command.AdminId,
            ReviewerRole = UserRoleType.Manager,
            FromStatus = fromStatus,
            ToStatus = TutorialStatus.Published,
            Action = "Publish",
            CreatedAt = DateTime.UtcNow
        }, ct);
    }
}
