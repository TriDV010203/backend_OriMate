using Moq;
using Xunit;
using OrigamiPlatform.Application.Queries.Tutorials;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Tests.Queries.Tutorials;

public class GetMyTutorialsHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockRepo;
    private readonly GetMyTutorialsHandler _handler;

    public GetMyTutorialsHandlerTests()
    {
        _mockRepo = new Mock<ITutorialRepository>();
        _handler = new GetMyTutorialsHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidQuery_ReturnsPagedResult()
    {
        var authorId = Guid.NewGuid();
        var query = new GetMyTutorialsQuery(authorId, 1, 10);
        
        var tutorials = new List<Tutorial>
        {
            new() 
            {
                Id = Guid.NewGuid(),
                Title = "My Title",
                Slug = "my-title",
                CoverImageUrl = "img.jpg",
                Type = TutorialType.Free,
                Difficulty = TutorialDifficulty.Beginner,
                Status = TutorialStatus.Published,
                Steps = new List<TutorialStep>()
            }
        };
        var pagedResult = new PagedResult<Tutorial>(tutorials, 1, 1, 10, 1);

        _mockRepo.Setup(r => r.GetByAuthorAsync(query.AuthorId, 1, 10, default))
            .ReturnsAsync(pagedResult);

        var result = await _handler.HandleAsync(query);

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("My Title", result.Items.First().Title);
    }
}
