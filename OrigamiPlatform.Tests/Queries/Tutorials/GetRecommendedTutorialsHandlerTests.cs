using Moq;
using Xunit;
using OrigamiPlatform.Application.Queries.Tutorials;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Tests.Queries.Tutorials;

public class GetRecommendedTutorialsHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockTutorials;
    private readonly Mock<IAchievementRepository> _mockAchievements;
    private readonly Mock<IUserRepository> _mockUsers;
    private readonly Mock<IVipSubscriptionRepository> _mockVip;
    private readonly Mock<ILikeRepository> _mockLikes;
    private readonly Mock<IWishlistRepository> _mockWishlists;
    private readonly Mock<ICommentRepository> _mockComments;
    private readonly GetRecommendedTutorialsHandler _handler;

    public GetRecommendedTutorialsHandlerTests()
    {
        _mockTutorials = new Mock<ITutorialRepository>();
        _mockAchievements = new Mock<IAchievementRepository>();
        _mockUsers = new Mock<IUserRepository>();
        _mockVip = new Mock<IVipSubscriptionRepository>();
        _mockLikes = new Mock<ILikeRepository>();
        _mockWishlists = new Mock<IWishlistRepository>();
        _mockComments = new Mock<ICommentRepository>();

        _handler = new GetRecommendedTutorialsHandler(
            _mockTutorials.Object,
            _mockAchievements.Object,
            _mockUsers.Object,
            _mockVip.Object,
            _mockLikes.Object,
            _mockWishlists.Object,
            _mockComments.Object
        );
    }

    [Fact]
    public async Task HandleAsync_AnonymousUser_ReturnsDefaultRecommendations()
    {
        var query = new GetRecommendedTutorialsQuery(null, 1, 10);
        
        var tutorials = new List<Tutorial>
        {
            new() 
            {
                Id = Guid.NewGuid(),
                Title = "Rec Title",
                Slug = "rec-title",
                CoverImageUrl = "img.jpg",
                Type = TutorialType.Free,
                Difficulty = TutorialDifficulty.Beginner,
                Status = TutorialStatus.Published,
                CategoryId = 1,
                Category = new Category { Name = "Cat" },
                Author = new User { Email = "test@test.com" },
                Steps = new List<TutorialStep>()
            }
        };
        var pagedResult = new PagedResult<Tutorial>(tutorials, 1, 1, 10, 1);

        _mockTutorials.Setup(r => r.GetRecommendedAsync(It.IsAny<List<int>>(), It.IsAny<TutorialDifficulty[]>(), It.IsAny<HashSet<Guid>>(), 1, 10, default))
            .ReturnsAsync(pagedResult);

        var result = await _handler.HandleAsync(query);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Rec Title", result.Items.First().Title);
    }
}
