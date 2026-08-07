using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.DailyChallenge;
using OrigamiPlatform.Application.DTOs.DailyChallenge;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.DailyChallenge;

public class AdminScheduleDailyChallengeHandlerTests
{
    private readonly Mock<IDailyChallengeRepository> _mockChallenges;
    private readonly Mock<ITutorialRepository> _mockTutorials;
    private readonly AdminScheduleDailyChallengeHandler _handler;

    public AdminScheduleDailyChallengeHandlerTests()
    {
        _mockChallenges = new Mock<IDailyChallengeRepository>();
        _mockTutorials = new Mock<ITutorialRepository>();
        _handler = new AdminScheduleDailyChallengeHandler(_mockChallenges.Object, _mockTutorials.Object);
    }

    [Fact]
    public async Task HandleAsync_PastDate_ThrowsDomainException()
    {
        var req = new ScheduleDailyChallengeRequest(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), Guid.NewGuid());
        var command = new AdminScheduleDailyChallengeCommand(Guid.NewGuid(), req);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("Không thể đặt lịch cho ngày trong quá khứ.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_TutorialNotFound_ThrowsNotFoundException()
    {
        var req = new ScheduleDailyChallengeRequest(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), Guid.NewGuid());
        var command = new AdminScheduleDailyChallengeCommand(Guid.NewGuid(), req);

        _mockTutorials.Setup(t => t.GetByIdWithStepsAsync(req.TutorialId, default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Equal("Tutorial not found.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_TutorialNotPublished_ThrowsDomainException()
    {
        var req = new ScheduleDailyChallengeRequest(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), Guid.NewGuid());
        var command = new AdminScheduleDailyChallengeCommand(Guid.NewGuid(), req);
        var tutorial = new Tutorial { Id = req.TutorialId, Status = TutorialStatus.Draft };

        _mockTutorials.Setup(t => t.GetByIdWithStepsAsync(req.TutorialId, default)).ReturnsAsync(tutorial);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("Chỉ có thể chọn tutorial đã ở trạng thái Published.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ExistingChallengeNotScheduled_ThrowsDomainException()
    {
        var req = new ScheduleDailyChallengeRequest(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), Guid.NewGuid());
        var command = new AdminScheduleDailyChallengeCommand(Guid.NewGuid(), req);
        var tutorial = new Tutorial { Id = req.TutorialId, Status = TutorialStatus.Published };
        var existing = new OrigamiPlatform.Domain.Entities.DailyChallenge { Status = DailyChallengeStatus.Active };

        _mockTutorials.Setup(t => t.GetByIdWithStepsAsync(req.TutorialId, default)).ReturnsAsync(tutorial);
        _mockChallenges.Setup(c => c.GetByDateAsync(req.ChallengeDate, default)).ReturnsAsync(existing);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("Không thể sửa lịch của ngày đã kích hoạt hoặc đã đóng.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequestNew_CreatesScheduledChallenge()
    {
        var req = new ScheduleDailyChallengeRequest(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), Guid.NewGuid());
        var command = new AdminScheduleDailyChallengeCommand(Guid.NewGuid(), req);
        var tutorial = new Tutorial { Id = req.TutorialId, Status = TutorialStatus.Published };

        _mockTutorials.Setup(t => t.GetByIdWithStepsAsync(req.TutorialId, default)).ReturnsAsync(tutorial);
        _mockChallenges.Setup(c => c.GetByDateAsync(req.ChallengeDate, default)).ReturnsAsync((OrigamiPlatform.Domain.Entities.DailyChallenge?)null);
        
        _mockChallenges.Setup(c => c.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid id, CancellationToken ct) => new OrigamiPlatform.Domain.Entities.DailyChallenge { Id = id, ChallengeDate = req.ChallengeDate, Tutorial = tutorial, Status = DailyChallengeStatus.Scheduled });

        var result = await _handler.HandleAsync(command);

        Assert.NotNull(result);
        Assert.Equal(DailyChallengeStatus.Scheduled.ToString(), result.Status);

        _mockChallenges.Verify(c => c.AddAsync(It.Is<OrigamiPlatform.Domain.Entities.DailyChallenge>(dc => 
            dc.ChallengeDate == req.ChallengeDate && 
            dc.TutorialId == req.TutorialId &&
            dc.Status == DailyChallengeStatus.Scheduled), default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ValidRequestExisting_UpdatesScheduledChallenge()
    {
        var req = new ScheduleDailyChallengeRequest(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), Guid.NewGuid());
        var command = new AdminScheduleDailyChallengeCommand(Guid.NewGuid(), req);
        var tutorial = new Tutorial { Id = req.TutorialId, Status = TutorialStatus.Published };
        var existing = new OrigamiPlatform.Domain.Entities.DailyChallenge { Id = Guid.NewGuid(), Status = DailyChallengeStatus.Scheduled, TutorialId = Guid.NewGuid(), Tutorial = tutorial };

        _mockTutorials.Setup(t => t.GetByIdWithStepsAsync(req.TutorialId, default)).ReturnsAsync(tutorial);
        _mockChallenges.Setup(c => c.GetByDateAsync(req.ChallengeDate, default)).ReturnsAsync(existing);
        
        _mockChallenges.Setup(c => c.GetByIdAsync(existing.Id, default))
            .ReturnsAsync(existing);

        var result = await _handler.HandleAsync(command);

        Assert.NotNull(result);

        _mockChallenges.Verify(c => c.UpdateAsync(It.Is<OrigamiPlatform.Domain.Entities.DailyChallenge>(dc => 
            dc.Id == existing.Id && 
            dc.TutorialId == req.TutorialId), default), Times.Once);
    }
}
