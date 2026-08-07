using Moq;
using Xunit;
using OrigamiPlatform.Application.Queries.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Queries.Tutorials;

public class GetTutorialBySlugHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockTutorials;
    private readonly Mock<IVipSubscriptionRepository> _mockVip;
    private readonly Mock<ILikeRepository> _mockLikes;
    private readonly Mock<IWishlistRepository> _mockWishlists;
    private readonly GetTutorialBySlugHandler _handler;

    public GetTutorialBySlugHandlerTests()
    {
        _mockTutorials = new Mock<ITutorialRepository>();
        _mockVip = new Mock<IVipSubscriptionRepository>();
        _mockLikes = new Mock<ILikeRepository>();
        _mockWishlists = new Mock<IWishlistRepository>();

        _handler = new GetTutorialBySlugHandler(
            _mockTutorials.Object,
            _mockVip.Object,
            _mockLikes.Object,
            _mockWishlists.Object
        );
    }

    [Fact]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        var query = new GetTutorialBySlugQuery("not-found", null);
        _mockTutorials.Setup(r => r.GetPublishedBySlugAsync("not-found", default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(query));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidQuery_ReturnsTutorialDetail()
    {
        var query = new GetTutorialBySlugQuery("my-tutorial", null);
        var tutorial = new Tutorial 
        {
            Id = Guid.NewGuid(),
            Title = "My Tutorial",
            Slug = "my-tutorial",
            CoverImageUrl = "img.jpg",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Beginner,
            CategoryId = 1,
            Category = new Category { Name = "Cat" },
            Author = new User { Email = "author@test.com" },
            Steps = new List<TutorialStep>()
        };

        _mockTutorials.Setup(r => r.GetPublishedBySlugAsync("my-tutorial", default)).ReturnsAsync(tutorial);

        var result = await _handler.HandleAsync(query);

        Assert.NotNull(result);
        Assert.Equal("My Tutorial", result.Title);
    }
}
