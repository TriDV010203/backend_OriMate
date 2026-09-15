using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Application.Queries.Tutorials;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;
using Xunit;

namespace OrigamiPlatform.Application.UnitTests.Tutorials;

/// <summary>
/// Unit tests for <see cref="GetTutorialBySlugHandler"/>.
/// Focused on business rules around VIP content visibility (BR-29).
/// Pattern : Arrange – Act – Assert.
/// Naming  : [MethodName]_[Scenario]_[ExpectedResult]
/// </summary>
public class GetTutorialBySlugHandlerTests
{
    // ──────────────────────────────────────────────
    // Shared mocks
    // ──────────────────────────────────────────────
    private readonly Mock<ITutorialRepository>                _tutorialsMock   = new();
    private readonly Mock<IVipSubscriptionRepository>         _vipMock         = new();
    private readonly Mock<ILikeRepository>                    _likesMock       = new();
    private readonly Mock<IWishlistRepository>                _wishlistsMock   = new();
    private readonly Mock<IAchievementRepository>             _achievementsMock = new();
    private readonly Mock<ITutorialDifficultyRatingRepository> _ratingsMock    = new();

    /// <summary>
    /// Creates the SUT with all optional dependencies injected so every mock
    /// is under our control. Set <paramref name="withOptional"/> to false to
    /// construct with only the 4 required args (matches the default constructor
    /// variant used in the first two tests).
    /// </summary>
    private GetTutorialBySlugHandler CreateSut(bool withOptional = true) =>
        withOptional
            ? new(_tutorialsMock.Object, _vipMock.Object, _likesMock.Object,
                  _wishlistsMock.Object, _achievementsMock.Object, _ratingsMock.Object)
            : new(_tutorialsMock.Object, _vipMock.Object, _likesMock.Object,
                  _wishlistsMock.Object);

    // ──────────────────────────────────────────────
    // Helpers – entity builders
    // ──────────────────────────────────────────────

    private static readonly Guid _creatorId = Guid.NewGuid();

    /// <summary>
    /// Builds a <see cref="Tutorial"/> with the given type and a configurable
    /// number of ordered steps so we can test step-level visibility rules.
    /// </summary>
    private static Tutorial BuildTutorial(TutorialType type, int stepCount = 4)
    {
        var steps = Enumerable.Range(1, stepCount)
            .Select(i => new TutorialStep
            {
                Id          = Guid.NewGuid(),
                TutorialId  = Guid.Empty,           // populated below
                StepOrder   = i,
                Description = $"Nội dung bước {i}",
                ImageUrl    = $"https://cdn.example.com/step{i}.jpg",
                CreatedAt   = DateTime.UtcNow
            })
            .ToList();

        var tutorial = new Tutorial
        {
            Id            = Guid.NewGuid(),
            AuthorId      = _creatorId,
            Title         = "Hướng dẫn Gấp Hạc Giấy",
            Slug          = "gap-hac-giay",
            Description   = "Học gấp hạc giấy từ cơ bản đến nâng cao.",
            CoverImageUrl = "https://cdn.example.com/cover.jpg",
            Type          = type,
            Difficulty    = TutorialDifficulty.Intermediate,
            Status        = TutorialStatus.Published,
            IsOfficial    = false,
            CategoryId    = 1,
            CreatedAt     = DateTime.UtcNow,
            PublishedAt   = DateTime.UtcNow,
            Category      = new Category { Id = 1, Name = "Origami Cơ Bản" },
            Author        = new User
            {
                Id      = _creatorId,
                Email   = "creator@example.com",
                Profile = new UserProfile { DisplayName = "Nghệ nhân Origami", CreatedAt = DateTime.UtcNow }
            },
            Steps = steps
        };

        // Backfill TutorialId on each step
        foreach (var s in steps) s.TutorialId = tutorial.Id;

        return tutorial;
    }

    /// <summary>Sets up all social-count mocks to return zero/null for any ID.</summary>
    private void SetupZeroSocialCounts(Guid tutorialId)
    {
        _likesMock
            .Setup(r => r.GetLikeCountAsync(tutorialId, TargetType.Tutorial))
            .ReturnsAsync(0);
        _wishlistsMock
            .Setup(r => r.GetWishlistCountAsync(tutorialId, TargetType.Tutorial, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _ratingsMock
            .Setup(r => r.GetCountsAsync(tutorialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<PerceivedDifficulty, int>());
        _achievementsMock
            .Setup(r => r.CountByTutorialAsync(tutorialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-01 : HandleAsync_FreeTutorial_ReturnsAllStepsVisible
    //         A Free tutorial must expose every step's Description and
    //         ImageUrl regardless of the user's subscription status.
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_FreeTutorial_ReturnsAllStepsVisible()
    {
        // ── Arrange ──────────────────────────────────────────────────
        const int stepCount = 4;
        var tutorial = BuildTutorial(TutorialType.Free, stepCount);
        var query    = new GetTutorialBySlugQuery(Slug: tutorial.Slug, CurrentUserId: null);

        _tutorialsMock
            .Setup(r => r.GetPublishedBySlugAsync(tutorial.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tutorial);

        SetupZeroSocialCounts(tutorial.Id);

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var result = await sut.HandleAsync(query);

        // ── Assert ────────────────────────────────────────────────────
        result.Should().NotBeNull();
        result.IsVipLocked.Should().BeFalse("a Free tutorial is never locked");

        var steps = result.Steps.ToList();
        steps.Should().HaveCount(stepCount);

        // Every step must be unlocked and contain real content
        steps.Should().AllSatisfy(s =>
        {
            s.IsLocked.Should().BeFalse();
            s.Description.Should().NotBeNullOrEmpty("description must be visible for Free tutorials");
            s.ImageUrl.Should().NotBeNullOrEmpty("image must be visible for Free tutorials");
        });

        // VIP subscription repo must NOT be consulted for a Free tutorial
        _vipMock.Verify(
            r => r.HasActiveSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-02 : HandleAsync_VipTutorial_UserIsVip_ReturnsAllStepsVisible
    //         A subscribed (VIP) user gets every step without locking.
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_VipTutorial_UserIsVip_ReturnsAllStepsVisible()
    {
        // ── Arrange ──────────────────────────────────────────────────
        const int stepCount = 5;
        var tutorial = BuildTutorial(TutorialType.VIP, stepCount);
        var userId   = Guid.NewGuid();
        var query    = new GetTutorialBySlugQuery(Slug: tutorial.Slug, CurrentUserId: userId);

        _tutorialsMock
            .Setup(r => r.GetPublishedBySlugAsync(tutorial.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tutorial);

        // ← VIP user IS subscribed to this creator
        _vipMock
            .Setup(r => r.HasActiveSubscriptionAsync(userId, tutorial.AuthorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _likesMock
            .Setup(r => r.GetLikeCountAsync(tutorial.Id, TargetType.Tutorial))
            .ReturnsAsync(0);
        _likesMock
            .Setup(r => r.GetLikeAsync(userId, tutorial.Id, TargetType.Tutorial))
            .ReturnsAsync((Like?)null);

        _wishlistsMock
            .Setup(r => r.GetWishlistCountAsync(tutorial.Id, TargetType.Tutorial, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _wishlistsMock
            .Setup(r => r.GetByUserAndTargetAsync(userId, tutorial.Id, TargetType.Tutorial, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wishlist?)null);

        _achievementsMock
            .Setup(r => r.GetByUserAndTutorialAsync(userId, tutorial.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Achievement?)null);
        _achievementsMock
            .Setup(r => r.CountByTutorialAsync(tutorial.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _ratingsMock
            .Setup(r => r.ExistsAsync(userId, tutorial.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ratingsMock
            .Setup(r => r.GetCountsAsync(tutorial.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<PerceivedDifficulty, int>());

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var result = await sut.HandleAsync(query);

        // ── Assert ────────────────────────────────────────────────────
        result.Should().NotBeNull();
        result.IsVipLocked.Should().BeFalse("a subscribed VIP user has full access");

        var steps = result.Steps.ToList();
        steps.Should().HaveCount(stepCount);

        steps.Should().AllSatisfy(s =>
        {
            s.IsLocked.Should().BeFalse();
            s.Description.Should().NotBeNullOrEmpty();
            s.ImageUrl.Should().NotBeNullOrEmpty();
        });
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-03 : HandleAsync_VipTutorial_UserIsFree_HidesLockedStepsData
    //
    //         CRITICAL BUSINESS RULE (BR-29):
    //         A non-subscriber viewing a VIP tutorial must see:
    //           • Steps 1..VipFreePreviewStepCount  → full content (unlocked)
    //           • Steps VipFreePreviewStepCount+1.. → IsLocked=true,
    //                                                  Description=string.Empty,
    //                                                  ImageUrl=null
    //         VipFreePreviewStepCount is currently 2 (see TutorialConstants).
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_VipTutorial_UserIsFree_HidesLockedStepsData()
    {
        // ── Arrange ──────────────────────────────────────────────────
        const int stepCount   = 5;
        const int freePreview = TutorialConstants.VipFreePreviewStepCount; // == 2

        var tutorial = BuildTutorial(TutorialType.VIP, stepCount);
        var userId   = Guid.NewGuid();
        var query    = new GetTutorialBySlugQuery(Slug: tutorial.Slug, CurrentUserId: userId);

        _tutorialsMock
            .Setup(r => r.GetPublishedBySlugAsync(tutorial.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tutorial);

        // ← Free user is NOT subscribed to this creator
        _vipMock
            .Setup(r => r.HasActiveSubscriptionAsync(userId, tutorial.AuthorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _likesMock
            .Setup(r => r.GetLikeCountAsync(tutorial.Id, TargetType.Tutorial))
            .ReturnsAsync(0);
        _likesMock
            .Setup(r => r.GetLikeAsync(userId, tutorial.Id, TargetType.Tutorial))
            .ReturnsAsync((Like?)null);

        _wishlistsMock
            .Setup(r => r.GetWishlistCountAsync(tutorial.Id, TargetType.Tutorial, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _wishlistsMock
            .Setup(r => r.GetByUserAndTargetAsync(userId, tutorial.Id, TargetType.Tutorial, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wishlist?)null);

        _achievementsMock
            .Setup(r => r.GetByUserAndTutorialAsync(userId, tutorial.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Achievement?)null);
        _achievementsMock
            .Setup(r => r.CountByTutorialAsync(tutorial.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _ratingsMock
            .Setup(r => r.ExistsAsync(userId, tutorial.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ratingsMock
            .Setup(r => r.GetCountsAsync(tutorial.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<PerceivedDifficulty, int>());

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var result = await sut.HandleAsync(query);

        // ── Assert ────────────────────────────────────────────────────
        result.Should().NotBeNull();
        result.IsVipLocked.Should().BeTrue("a non-subscriber must see the tutorial as locked");

        var steps = result.Steps.OrderBy(s => s.StepOrder).ToList();
        steps.Should().HaveCount(stepCount);

        // Steps within the free preview window → fully visible
        var freeSteps = steps.Where(s => s.StepOrder <= freePreview).ToList();
        freeSteps.Should().HaveCount(freePreview);
        freeSteps.Should().AllSatisfy(s =>
        {
            s.IsLocked.Should().BeFalse($"step {s.StepOrder} is within the free preview window");
            s.Description.Should().NotBeNullOrEmpty($"step {s.StepOrder} description must be visible");
            s.ImageUrl.Should().NotBeNullOrEmpty($"step {s.StepOrder} image must be visible");
        });

        // Steps beyond the free preview window → data must be stripped
        var lockedSteps = steps.Where(s => s.StepOrder > freePreview).ToList();
        lockedSteps.Should().HaveCount(stepCount - freePreview,
            "all steps beyond the preview window should be locked");
        lockedSteps.Should().AllSatisfy(s =>
        {
            s.IsLocked.Should().BeTrue($"step {s.StepOrder} must be locked for non-subscribers");
            s.Description.Should().BeEmpty(
                $"step {s.StepOrder} description must be stripped (empty string) for non-subscribers");
            s.ImageUrl.Should().BeNull(
                $"step {s.StepOrder} image must be stripped (null) for non-subscribers");
        });
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-04 : HandleAsync_VipTutorial_AnonymousUser_HidesLockedStepsData
    //         An unauthenticated visitor must get the same lock treatment
    //         as a logged-in free user (no CurrentUserId provided).
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_VipTutorial_AnonymousUser_HidesLockedStepsData()
    {
        // ── Arrange ──────────────────────────────────────────────────
        const int stepCount   = 3;
        const int freePreview = TutorialConstants.VipFreePreviewStepCount; // == 2

        var tutorial = BuildTutorial(TutorialType.VIP, stepCount);
        var query    = new GetTutorialBySlugQuery(Slug: tutorial.Slug, CurrentUserId: null);

        _tutorialsMock
            .Setup(r => r.GetPublishedBySlugAsync(tutorial.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tutorial);

        // No userId → VIP check should be skipped; anonymous is always unlocked=false
        SetupZeroSocialCounts(tutorial.Id);

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var result = await sut.HandleAsync(query);

        // ── Assert ────────────────────────────────────────────────────
        result.IsVipLocked.Should().BeTrue();

        var steps = result.Steps.OrderBy(s => s.StepOrder).ToList();

        steps.Where(s => s.StepOrder <= freePreview)
            .Should().AllSatisfy(s => s.IsLocked.Should().BeFalse());

        steps.Where(s => s.StepOrder > freePreview)
            .Should().AllSatisfy(s =>
            {
                s.IsLocked.Should().BeTrue();
                s.Description.Should().BeEmpty();
                s.ImageUrl.Should().BeNull();
            });

        // VIP repo must never be called for an anonymous visitor (no userId to check)
        _vipMock.Verify(
            r => r.HasActiveSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-05 : HandleAsync_TutorialNotFound_ThrowsNotFoundException
    //         When the slug does not match any published tutorial the
    //         handler must throw NotFoundException, never return null.
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_TutorialNotFound_ThrowsNotFoundException()
    {
        // ── Arrange ──────────────────────────────────────────────────
        const string slug  = "non-existent-slug";
        var          query = new GetTutorialBySlugQuery(Slug: slug);

        _tutorialsMock
            .Setup(r => r.GetPublishedBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tutorial?)null);   // ← tutorial does not exist

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var act = () => sut.HandleAsync(query);

        // ── Assert ────────────────────────────────────────────────────
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{slug}*");

        // No downstream repositories should be consulted after a 404
        _vipMock.Verify(
            r => r.HasActiveSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _likesMock.Verify(
            r => r.GetLikeCountAsync(It.IsAny<Guid>(), It.IsAny<TargetType>()),
            Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-06 : HandleAsync_VipTutorial_UserIsFree_ExactlyOnFreePreviewBoundary
    //         Edge case: a VIP tutorial that has exactly VipFreePreviewStepCount
    //         steps (== 2) should show ALL steps as unlocked — there are no
    //         extra steps to lock.
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_VipTutorial_UserIsFree_ExactlyOnFreePreviewBoundary_AllStepsVisible()
    {
        // ── Arrange ──────────────────────────────────────────────────
        const int stepCount   = TutorialConstants.VipFreePreviewStepCount; // == 2
        var tutorial = BuildTutorial(TutorialType.VIP, stepCount);
        var userId   = Guid.NewGuid();
        var query    = new GetTutorialBySlugQuery(Slug: tutorial.Slug, CurrentUserId: userId);

        _tutorialsMock
            .Setup(r => r.GetPublishedBySlugAsync(tutorial.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tutorial);

        _vipMock
            .Setup(r => r.HasActiveSubscriptionAsync(userId, tutorial.AuthorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Free user

        _likesMock
            .Setup(r => r.GetLikeCountAsync(tutorial.Id, TargetType.Tutorial))
            .ReturnsAsync(0);
        _likesMock
            .Setup(r => r.GetLikeAsync(userId, tutorial.Id, TargetType.Tutorial))
            .ReturnsAsync((Like?)null);

        _wishlistsMock
            .Setup(r => r.GetWishlistCountAsync(tutorial.Id, TargetType.Tutorial, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _wishlistsMock
            .Setup(r => r.GetByUserAndTargetAsync(userId, tutorial.Id, TargetType.Tutorial, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wishlist?)null);

        _achievementsMock
            .Setup(r => r.GetByUserAndTutorialAsync(userId, tutorial.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Achievement?)null);
        _achievementsMock
            .Setup(r => r.CountByTutorialAsync(tutorial.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _ratingsMock
            .Setup(r => r.ExistsAsync(userId, tutorial.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ratingsMock
            .Setup(r => r.GetCountsAsync(tutorial.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<PerceivedDifficulty, int>());

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var result = await sut.HandleAsync(query);

        // ── Assert ────────────────────────────────────────────────────
        // Tutorial header says VipLocked=true (the tutorial type is VIP and user hasn't subscribed)
        result.IsVipLocked.Should().BeTrue();

        var steps = result.Steps.ToList();
        steps.Should().HaveCount(stepCount);

        // But because all 2 steps fall within the free preview window, NONE are actually locked
        steps.Should().AllSatisfy(s =>
        {
            s.IsLocked.Should().BeFalse(
                "all steps are within the free preview window so no step data is hidden");
            s.Description.Should().NotBeEmpty();
            s.ImageUrl.Should().NotBeNull();
        });
    }
}
