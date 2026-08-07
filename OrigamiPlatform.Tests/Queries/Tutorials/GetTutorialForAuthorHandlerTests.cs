using Moq;
using Xunit;
using OrigamiPlatform.Application.Queries.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Queries.Tutorials;

public class GetTutorialForAuthorHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockRepo;
    private readonly GetTutorialForAuthorHandler _handler;

    public GetTutorialForAuthorHandlerTests()
    {
        _mockRepo = new Mock<ITutorialRepository>();
        _handler = new GetTutorialForAuthorHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        var query = new GetTutorialForAuthorQuery(Guid.NewGuid(), Guid.NewGuid());
        _mockRepo.Setup(r => r.GetByIdWithStepsAsync(query.TutorialId, default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(query));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_AuthorMismatch_ThrowsForbiddenException()
    {
        var query = new GetTutorialForAuthorQuery(Guid.NewGuid(), Guid.NewGuid());
        var tutorial = new Tutorial { Id = query.TutorialId, AuthorId = Guid.NewGuid() };
        _mockRepo.Setup(r => r.GetByIdWithStepsAsync(query.TutorialId, default)).ReturnsAsync(tutorial);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _handler.HandleAsync(query));
        Assert.Contains("not the author", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidQuery_ReturnsAuthorDetail()
    {
        var query = new GetTutorialForAuthorQuery(Guid.NewGuid(), Guid.NewGuid());
        var tutorial = new Tutorial 
        {
            Id = query.TutorialId,
            AuthorId = query.AuthorId,
            Title = "Author Tutorial",
            Slug = "author-tutorial",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Beginner,
            Status = TutorialStatus.Draft,
            CategoryId = 1,
            Steps = new List<TutorialStep>()
        };

        _mockRepo.Setup(r => r.GetByIdWithStepsAsync(query.TutorialId, default)).ReturnsAsync(tutorial);

        var result = await _handler.HandleAsync(query);

        Assert.NotNull(result);
        Assert.Equal("Author Tutorial", result.Title);
    }
}
