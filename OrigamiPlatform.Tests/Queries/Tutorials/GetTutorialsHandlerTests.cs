using Moq;
using Xunit;
using OrigamiPlatform.Application.Queries.Tutorials;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Queries.Tutorials;

public class GetTutorialsHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockTutorials;
    private readonly Mock<IVipSubscriptionRepository> _mockVip;
    private readonly Mock<IUserRepository> _mockUsers;
    private readonly Mock<ILikeRepository> _mockLikes;
    private readonly Mock<IWishlistRepository> _mockWishlists;
    private readonly Mock<ICommentRepository> _mockComments;
    private readonly GetTutorialsHandler _handler;

    public GetTutorialsHandlerTests()
    {
        _mockTutorials = new Mock<ITutorialRepository>();
        _mockVip = new Mock<IVipSubscriptionRepository>();
        _mockUsers = new Mock<IUserRepository>();
        _mockLikes = new Mock<ILikeRepository>();
        _mockWishlists = new Mock<IWishlistRepository>();
        _mockComments = new Mock<ICommentRepository>();

        _handler = new GetTutorialsHandler(
            _mockTutorials.Object,
            _mockVip.Object,
            _mockUsers.Object,
            _mockLikes.Object,
            _mockWishlists.Object,
            _mockComments.Object
        );
    }

    [Fact]
    public async Task HandleAsync_InvalidType_ThrowsDomainException()
    {
        var query = new GetTutorialsQuery { Type = "Invalid" };

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(query));
        Assert.Contains("Invalid type", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidSortBy_ThrowsDomainException()
    {
        var query = new GetTutorialsQuery { SortBy = "Invalid" };

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(query));
        Assert.Contains("Invalid sortBy", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidQuery_ReturnsPagedResult()
    {
        var query = new GetTutorialsQuery { Page = 1, PageSize = 10, SortBy = "date" };
        
        var tutorials = new List<Tutorial>
        {
            new() 
            {
                Id = Guid.NewGuid(),
                Title = "Published Title",
                Slug = "published-title",
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

        _mockTutorials.Setup(r => r.GetPublishedAsync(null, null, null, null, "date", 1, 10, null, default))
            .ReturnsAsync((tutorials, 1));

        var result = await _handler.HandleAsync(query);

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Published Title", result.Items.First().Title);
    }
}
