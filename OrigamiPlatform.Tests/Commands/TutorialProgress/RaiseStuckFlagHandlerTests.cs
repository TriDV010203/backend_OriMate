using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.TutorialProgress;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.TutorialProgress;

public class RaiseStuckFlagHandlerTests
{
    private readonly Mock<ITutorialStepProgressRepository> _mockSteps;
    private readonly Mock<IStuckThreadRepository> _mockStuckThreads;
    private readonly RaiseStuckFlagHandler _handler;

    public RaiseStuckFlagHandlerTests()
    {
        _mockSteps = new Mock<ITutorialStepProgressRepository>();
        _mockStuckThreads = new Mock<IStuckThreadRepository>();
        _handler = new RaiseStuckFlagHandler(_mockSteps.Object, _mockStuckThreads.Object);
    }

    [Fact]
    public async Task HandleAsync_StepNotFound_ThrowsNotFoundException()
    {
        var command = new RaiseStuckFlagCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _mockSteps.Setup(s => s.GetStepWithTutorialAsync(command.StepId, default)).ReturnsAsync((TutorialStep?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Equal("Tutorial step not found.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_StepDoesNotBelongToTutorial_ThrowsNotFoundException()
    {
        var command = new RaiseStuckFlagCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var step = new TutorialStep { Id = command.StepId, TutorialId = Guid.NewGuid() }; // Different TutorialId

        _mockSteps.Setup(s => s.GetStepWithTutorialAsync(command.StepId, default)).ReturnsAsync(step);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Equal("This step does not belong to the given tutorial.", ex.Message);
    }

    [Theory]
    [InlineData(TutorialStatus.Draft, false)]
    [InlineData(TutorialStatus.Published, true)]
    public async Task HandleAsync_InvalidTutorialStatusOrDeleted_ThrowsDomainException(TutorialStatus status, bool isDeleted)
    {
        var command = new RaiseStuckFlagCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var step = new TutorialStep
        {
            Id = command.StepId,
            TutorialId = command.TutorialId,
            Tutorial = new Tutorial { Status = status, IsDeleted = isDeleted }
        };

        _mockSteps.Setup(s => s.GetStepWithTutorialAsync(command.StepId, default)).ReturnsAsync(step);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("You can only raise a stuck flag on a published tutorial.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ExistingThread_ReturnsExistingWithoutAdding()
    {
        var command = new RaiseStuckFlagCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var step = new TutorialStep
        {
            Id = command.StepId,
            TutorialId = command.TutorialId,
            Tutorial = new Tutorial { Status = TutorialStatus.Published, IsDeleted = false }
        };
        var existingThread = new StuckThread { Id = Guid.NewGuid(), TutorialId = command.TutorialId, StepId = command.StepId, UserId = command.UserId };

        _mockSteps.Setup(s => s.GetStepWithTutorialAsync(command.StepId, default)).ReturnsAsync(step);
        _mockStuckThreads.Setup(t => t.GetByUserAndStepAsync(command.UserId, command.StepId, default)).ReturnsAsync(existingThread);

        var result = await _handler.HandleAsync(command);

        Assert.Equal(existingThread.Id, result.Id);
        _mockStuckThreads.Verify(t => t.AddAsync(It.IsAny<StuckThread>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_AddsNewThreadAndReturnsDto()
    {
        var command = new RaiseStuckFlagCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var step = new TutorialStep
        {
            Id = command.StepId,
            TutorialId = command.TutorialId,
            Tutorial = new Tutorial { Status = TutorialStatus.Published, IsDeleted = false }
        };

        _mockSteps.Setup(s => s.GetStepWithTutorialAsync(command.StepId, default)).ReturnsAsync(step);
        _mockStuckThreads.Setup(t => t.GetByUserAndStepAsync(command.UserId, command.StepId, default)).ReturnsAsync((StuckThread?)null);

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(command.TutorialId, result.TutorialId);
        Assert.Equal(command.StepId, result.StepId);
        Assert.Equal(command.UserId, result.UserId);

        _mockStuckThreads.Verify(t => t.AddAsync(It.Is<StuckThread>(st =>
            st.TutorialId == command.TutorialId &&
            st.StepId == command.StepId &&
            st.UserId == command.UserId
        ), default), Times.Once);
    }
}
