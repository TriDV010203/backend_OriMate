using Moq;
using Xunit;
using OrigamiPlatform.Application.Queries.Tutorials;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Queries.Tutorials;

public class GetAdminTutorialsHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockRepo;
    private readonly GetAdminTutorialsHandler _handler;

    public GetAdminTutorialsHandlerTests()
    {
        _mockRepo = new Mock<ITutorialRepository>();
        _handler = new GetAdminTutorialsHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_InvalidStatus_ThrowsDomainException()
    {
        var query = new GetAdminTutorialsQuery(null, "InvalidStatus", null, null, 1, 10);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(query));
        Assert.Contains("Invalid status", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidQuery_ReturnsPagedResult()
    {
        var query = new GetAdminTutorialsQuery(null, "Published", null, null, 1, 10);
        
        var tutorials = new List<Tutorial>
        {
            new() 
            {
                Id = Guid.NewGuid(),
                Title = "Title",
                Slug = "slug",
                CoverImageUrl = "img.jpg",
                Type = TutorialType.Free,
                Difficulty = TutorialDifficulty.Beginner,
                Status = TutorialStatus.Published,
                CategoryId = 1,
                Category = new Category { Name = "Cat" },
                Author = new User { Email = "test@test.com" },
                IsOfficial = true,
                Steps = new List<TutorialStep>()
            }
        };
        var pagedResult = new PagedResult<Tutorial>(tutorials, 1, 1, 10, 1);

        _mockRepo.Setup(r => r.GetAllForAdminAsync(query.Search, TutorialStatus.Published, query.CategoryId, query.IsOfficial, 1, 10, default))
            .ReturnsAsync(pagedResult);

        var result = await _handler.HandleAsync(query);

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Title", result.Items.First().Title);
    }
}
