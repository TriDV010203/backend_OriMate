using Moq;
using Xunit;
using OrigamiPlatform.Application.Queries.Tutorials;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Tests.Queries.Tutorials;

public class GetManagerQueueHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockRepo;
    private readonly GetManagerQueueHandler _handler;

    public GetManagerQueueHandlerTests()
    {
        _mockRepo = new Mock<ITutorialRepository>();
        _handler = new GetManagerQueueHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidQuery_ReturnsPagedResult()
    {
        var query = new GetManagerQueueQuery(1, 10);
        
        var tutorials = new List<Tutorial>
        {
            new() 
            {
                Id = Guid.NewGuid(),
                Title = "Queue Item",
                Slug = "queue-item",
                Author = new User { Email = "author@test.com" },
                Steps = new List<TutorialStep>(),
                ParentTutorialId = null
            }
        };
        var pagedResult = new PagedResult<Tutorial>(tutorials, 1, 1, 10, 1);

        _mockRepo.Setup(r => r.GetPendingManagerReviewAsync(1, 10, default))
            .ReturnsAsync(pagedResult);

        var result = await _handler.HandleAsync(query);

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Queue Item", result.Items.First().Title);
        Assert.False(result.Items.First().IsEdit);
    }
}
