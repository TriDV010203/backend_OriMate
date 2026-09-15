using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Application.Queries.Tutorials;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;
using Xunit;

namespace OrigamiPlatform.Application.UnitTests.Tutorials;

/// <summary>
/// Unit tests for <see cref="GetTutorialsHandler"/>.
/// Pattern : Arrange – Act – Assert.
/// Naming  : [MethodName]_[Scenario]_[ExpectedResult]
/// </summary>
public class GetTutorialsHandlerTests
{
    // ──────────────────────────────────────────────
    // Shared mocks
    // ──────────────────────────────────────────────
    private readonly Mock<ITutorialRepository>       _tutorialsMock       = new();
    private readonly Mock<IVipSubscriptionRepository> _vipMock            = new();
    private readonly Mock<IUserRepository>            _usersMock          = new();
    private readonly Mock<ILikeRepository>            _likesMock          = new();
    private readonly Mock<IWishlistRepository>        _wishlistsMock      = new();
    private readonly Mock<ICommentRepository>         _commentsMock       = new();

    private GetTutorialsHandler CreateSut() =>
        new(_tutorialsMock.Object,
            _vipMock.Object,
            _usersMock.Object,
            _likesMock.Object,
            _wishlistsMock.Object,
            _commentsMock.Object);

    // ──────────────────────────────────────────────
    // Helpers – minimal entity builders
    // ──────────────────────────────────────────────

    private static Tutorial BuildPublishedTutorial(
        TutorialType type = TutorialType.Free,
        int categoryId    = 1,
        string categoryName = "Origami Cơ Bản") => new()
    {
        Id          = Guid.NewGuid(),
        Title       = "Test Tutorial",
        Slug        = "test-tutorial",
        Description = "A test tutorial description.",
        CoverImageUrl = "https://cdn.example.com/cover.jpg",
        Type        = type,
        Difficulty  = TutorialDifficulty.Beginner,
        Status      = TutorialStatus.Published,
        IsOfficial  = false,
        CategoryId  = categoryId,
        CreatedAt   = DateTime.UtcNow,
        PublishedAt = DateTime.UtcNow,
        Category    = new Category { Id = categoryId, Name = categoryName },
        Author = new User
        {
            Id    = Guid.NewGuid(),
            Email = "creator@example.com",
            Profile = new UserProfile { DisplayName = "Tác giả", CreatedAt = DateTime.UtcNow }
        },
        Steps = new List<TutorialStep>
        {
            new() { Id = Guid.NewGuid(), StepOrder = 1, Description = "Bước 1" }
        }
    };

    /// <summary>Sets up all social-count mocks to return zero for any tutorial ID.</summary>
    private void SetupZeroSocialCounts()
    {
        _likesMock
            .Setup(r => r.GetLikeCountAsync(It.IsAny<Guid>(), TargetType.Tutorial))
            .ReturnsAsync(0);
        _wishlistsMock
            .Setup(r => r.GetWishlistCountAsync(It.IsAny<Guid>(), TargetType.Tutorial, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _commentsMock
            .Setup(r => r.GetCommentCountAsync(It.IsAny<Guid>(), TargetType.Tutorial, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-01 : HandleAsync_ValidFilters_ReturnsPagedTutorials
    //         Happy path – basic search + category filter, anonymous user.
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_ValidFilters_ReturnsPagedTutorials()
    {
        // ── Arrange ──────────────────────────────────────────────────
        var tutorial  = BuildPublishedTutorial();
        var tutorials = new List<Tutorial> { tutorial };
        const int totalCount = 1;

        var query = new GetTutorialsQuery(
            Search:     "test",
            CategoryId: 1,
            Difficulty: "Beginner",
            Type:       "Free",
            SortBy:     "date",
            Page:       1,
            PageSize:   10,
            CurrentUserId: null           // anonymous user
        );

        _tutorialsMock
            .Setup(r => r.GetPublishedAsync(
                query.Search,
                query.CategoryId,
                TutorialDifficulty.Beginner,
                TutorialType.Free,
                "date",
                1,
                10,
                null,                     // no followedCreatorIds for anonymous
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((tutorials.AsEnumerable(), totalCount));

        SetupZeroSocialCounts();

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var result = await sut.HandleAsync(query);

        // ── Assert ────────────────────────────────────────────────────
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(totalCount);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);

        var dto = result.Items.Single();
        dto.Title.Should().Be(tutorial.Title);
        dto.Slug.Should().Be(tutorial.Slug);
        dto.Type.Should().Be(TutorialType.Free.ToString());
        dto.CategoryName.Should().Be(tutorial.Category.Name);
        dto.StepCount.Should().Be(1);

        // Anonymous user ⇒ no VIP lock on a Free tutorial
        dto.IsVipLocked.Should().BeFalse();
        dto.IsLikedByCurrentUser.Should().BeFalse();
        dto.IsWishlistedByCurrentUser.Should().BeFalse();

        // Ensure repository was queried exactly once
        _tutorialsMock.Verify(r => r.GetPublishedAsync(
            It.IsAny<string?>(),
            It.IsAny<int?>(),
            It.IsAny<TutorialDifficulty?>(),
            It.IsAny<TutorialType?>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<IReadOnlySet<Guid>?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-02 : HandleAsync_EmptyResult_ReturnsEmptyList
    //         Repository returns no tutorials → handler propagates an
    //         empty paged result without throwing.
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_EmptyResult_ReturnsEmptyList()
    {
        // ── Arrange ──────────────────────────────────────────────────
        var query = new GetTutorialsQuery(Search: "nonexistent keyword");

        _tutorialsMock
            .Setup(r => r.GetPublishedAsync(
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<TutorialDifficulty?>(),
                It.IsAny<TutorialType?>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<IReadOnlySet<Guid>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enumerable.Empty<Tutorial>(), 0));

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var result = await sut.HandleAsync(query);

        // ── Assert ────────────────────────────────────────────────────
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);

        // No social-count calls are expected when there are no items
        _likesMock.Verify(
            r => r.GetLikeCountAsync(It.IsAny<Guid>(), It.IsAny<TargetType>()),
            Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-03 : HandleAsync_InvalidType_ThrowsDomainException
    //         An unrecognised value for the `type` filter must throw
    //         DomainException (not crash with a raw FormatException).
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_InvalidType_ThrowsDomainException()
    {
        // ── Arrange ──────────────────────────────────────────────────
        var query = new GetTutorialsQuery(Type: "Premium");   // invalid enum value
        var sut   = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var act = () => sut.HandleAsync(query);

        // ── Assert ────────────────────────────────────────────────────
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Invalid type*");

        // Repository must never be called if validation fails early
        _tutorialsMock.Verify(
            r => r.GetPublishedAsync(
                It.IsAny<string?>(), It.IsAny<int?>(),
                It.IsAny<TutorialDifficulty?>(), It.IsAny<TutorialType?>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<IReadOnlySet<Guid>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-04 : HandleAsync_InvalidSortBy_ThrowsDomainException
    //         Unsupported sortBy values must be rejected before any DB call.
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_InvalidSortBy_ThrowsDomainException()
    {
        // ── Arrange ──────────────────────────────────────────────────
        var query = new GetTutorialsQuery(SortBy: "rating");  // only "date"/"likes" are valid
        var sut   = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var act = () => sut.HandleAsync(query);

        // ── Assert ────────────────────────────────────────────────────
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Invalid sortBy*");
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-05 : HandleAsync_InvalidDifficulty_ReturnsEmptyList
    //         Per business rule: an unparseable difficulty is lenient and
    //         returns an empty result instead of a 400 error.
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_InvalidDifficulty_ReturnsEmptyList()
    {
        // ── Arrange ──────────────────────────────────────────────────
        var query = new GetTutorialsQuery(Difficulty: "SuperEasy"); // invalid enum value
        var sut   = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var result = await sut.HandleAsync(query);

        // ── Assert ────────────────────────────────────────────────────
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty("an invalid difficulty should silently return empty, not throw");
        result.TotalCount.Should().Be(0);

        // Repository is short-circuited; no DB call should have been made
        _tutorialsMock.Verify(
            r => r.GetPublishedAsync(
                It.IsAny<string?>(), It.IsAny<int?>(),
                It.IsAny<TutorialDifficulty?>(), It.IsAny<TutorialType?>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<IReadOnlySet<Guid>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-06 : HandleAsync_AuthenticatedUser_EnrichesIsLikedAndIsWishlisted
    //         When a logged-in user is provided, their like/wishlist state
    //         must be reflected accurately in the returned DTOs.
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_AuthenticatedUser_EnrichesIsLikedAndIsWishlisted()
    {
        // ── Arrange ──────────────────────────────────────────────────
        var userId    = Guid.NewGuid();
        var tutorial  = BuildPublishedTutorial();
        var tutorials = new List<Tutorial> { tutorial };

        var query = new GetTutorialsQuery(
            SortBy:        "date",
            Page:          1,
            PageSize:      10,
            CurrentUserId: userId);

        _usersMock
            .Setup(r => r.GetFollowingIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid>());

        _vipMock
            .Setup(r => r.GetSubscribedCreatorIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid>());

        _tutorialsMock
            .Setup(r => r.GetPublishedAsync(
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<TutorialDifficulty?>(),
                It.IsAny<TutorialType?>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<IReadOnlySet<Guid>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((tutorials.AsEnumerable(), 1));

        // User has liked and wishlisted this tutorial
        _likesMock
            .Setup(r => r.GetLikeCountAsync(tutorial.Id, TargetType.Tutorial))
            .ReturnsAsync(5);
        _likesMock
            .Setup(r => r.GetLikeAsync(userId, tutorial.Id, TargetType.Tutorial))
            .ReturnsAsync(new Like { UserId = userId, TargetId = tutorial.Id });

        _wishlistsMock
            .Setup(r => r.GetWishlistCountAsync(tutorial.Id, TargetType.Tutorial, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _wishlistsMock
            .Setup(r => r.GetByUserAndTargetAsync(userId, tutorial.Id, TargetType.Tutorial, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Wishlist { UserId = userId, TargetId = tutorial.Id });

        _commentsMock
            .Setup(r => r.GetCommentCountAsync(tutorial.Id, TargetType.Tutorial, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var result = await sut.HandleAsync(query);

        // ── Assert ────────────────────────────────────────────────────
        var dto = result.Items.Single();
        dto.LikeCount.Should().Be(5);
        dto.WishlistCount.Should().Be(3);
        dto.CommentCount.Should().Be(2);
        dto.IsLikedByCurrentUser.Should().BeTrue();
        dto.IsWishlistedByCurrentUser.Should().BeTrue();
    }
}
