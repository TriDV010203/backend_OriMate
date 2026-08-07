using Moq;
using Xunit;
using OrigamiPlatform.Application.Queries.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Queries.Tutorials;

public class GetTutorialForAdminHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockRepo;
    private readonly GetTutorialForAdminHandler _handler;

    public GetTutorialForAdminHandlerTests()
    {
        _mockRepo = new Mock<ITutorialRepository>();
        _handler = new GetTutorialForAdminHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        var tutorialId = Guid.NewGuid();
        var query = new GetTutorialForAdminQuery(tutorialId);
        _mockRepo.Setup(r => r.GetByIdWithStepsAsync(query.TutorialId, default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(query));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidQuery_ReturnsAdminDetail()
    {
        var tutorialId = Guid.NewGuid();
        var query = new GetTutorialForAdminQuery(tutorialId);
        var tutorial = new Tutorial 
        {
            Id = query.TutorialId,
            Title = "Admin Tutorial",
            Slug = "admin-tutorial",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Beginner,
            Status = TutorialStatus.Published,
            CategoryId = 1,
            Author = new User { Email = "admin@test.com" },
            Steps = new List<TutorialStep>()
        };

        _mockRepo.Setup(r => r.GetByIdWithStepsAsync(query.TutorialId, default)).ReturnsAsync(tutorial);

        var result = await _handler.HandleAsync(query);

        Assert.NotNull(result);
        Assert.Equal("Admin Tutorial", result.Title);
    }
}
