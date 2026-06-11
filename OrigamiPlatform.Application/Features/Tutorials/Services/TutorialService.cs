using System.Text.RegularExpressions;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Features.Tutorials.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Features.Tutorials.Services;

public class TutorialService : ITutorialService
{
    private readonly ITutorialRepository _tutorialRepo;
    private readonly INotificationService _notifications;
    private readonly IBlockedWordService _blockedWords;

    public TutorialService(
        ITutorialRepository tutorialRepo,
        INotificationService notifications,
        IBlockedWordService blockedWords)
    {
        _tutorialRepo = tutorialRepo;
        _notifications = notifications;
        _blockedWords = blockedWords;
    }

    public async Task<TutorialResponse> CreateDraftAsync(
        CreateTutorialRequest request, Guid authorId, CancellationToken ct = default)
    {
        if (request.Title.Length < 5 || request.Title.Length > 150)
            throw new DomainException("Title must be between 5 and 150 characters. BR-12.");
        if (request.Description.Length < 20 || request.Description.Length > 500)
            throw new DomainException("Description must be between 20 and 500 characters. BR-12.");

        // BR-23: blocked word check on user-supplied text
        if (await _blockedWords.ContainsBlockedWordAsync(request.Title, ct))
            throw new DomainException("Title contains a blocked word. BR-23.");
        if (await _blockedWords.ContainsBlockedWordAsync(request.Description, ct))
            throw new DomainException("Description contains a blocked word. BR-23.");

        if (!Enum.TryParse<TutorialType>(request.Type, ignoreCase: true, out var tutorialType))
            throw new DomainException($"Invalid tutorial type '{request.Type}'. Valid values: Free, VIP.");

        // BR-13: VIP requires active CreatorVipSettings
        if (tutorialType == TutorialType.VIP)
        {
            var vipSettings = await _tutorialRepo.GetActiveCreatorVipSettingsAsync(authorId, ct);
            if (vipSettings is null)
                throw new DomainException(
                    "You must configure a VIP pricing tier before creating VIP tutorials. BR-13.");
        }

        var category = await _tutorialRepo.GetActiveCategoryAsync(request.CategoryId, ct);
        if (category is null)
            throw new DomainException($"Category {request.CategoryId} does not exist or is not active.");

        var slug = await GenerateUniqueSlugAsync(request.Title, ct);

        var tutorial = new Tutorial
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            CategoryId = request.CategoryId,
            Title = request.Title,
            Description = request.Description,
            Slug = slug,
            CoverImageUrl = request.CoverImageUrl,
            Type = tutorialType,
            Difficulty = request.Difficulty,
            Status = TutorialStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        if (request.Steps is { Count: > 0 })
        {
            foreach (var stepReq in request.Steps)
            {
                if (!string.IsNullOrWhiteSpace(stepReq.Description)
                    && await _blockedWords.ContainsBlockedWordAsync(stepReq.Description, ct))
                    throw new DomainException($"Step {stepReq.StepOrder} description contains a blocked word. BR-23.");

                tutorial.Steps.Add(new TutorialStep
                {
                    Id = Guid.NewGuid(),
                    TutorialId = tutorial.Id,
                    StepOrder = stepReq.StepOrder,
                    Title = $"Step {stepReq.StepOrder}",
                    Content = stepReq.Description,
                    MediaUrl = stepReq.ImageUrl,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _tutorialRepo.AddAsync(tutorial, ct);
        return MapToResponse(tutorial);
    }

    public async Task<TutorialResponse> SubmitForReviewAsync(
        Guid tutorialId, Guid authorId, CancellationToken ct = default)
    {
        var tutorial = await _tutorialRepo.GetByIdWithStepsAsync(tutorialId, ct)
            ?? throw new NotFoundException($"Tutorial {tutorialId} not found.");

        if (tutorial.AuthorId != authorId)
            throw new ForbiddenException("You are not the author of this tutorial.");

        if (tutorial.Status != TutorialStatus.Draft && tutorial.Status != TutorialStatus.RevisionRequired)
            throw new DomainException("Tutorial cannot be submitted in its current status.");

        // BR-12 — collect all failures before throwing
        var errors = new List<string>();

        if (tutorial.Title.Length < 5 || tutorial.Title.Length > 150)
            errors.Add("Title must be between 5 and 150 characters.");
        if (tutorial.Description.Length < 20 || tutorial.Description.Length > 500)
            errors.Add("Description must be between 20 and 500 characters.");
        if (string.IsNullOrEmpty(tutorial.CoverImageUrl))
            errors.Add("Cover image is required.");

        var category = await _tutorialRepo.GetActiveCategoryAsync(tutorial.CategoryId, ct);
        if (category is null)
            errors.Add("Category does not exist or is not active.");

        if (tutorial.Steps.Count < 3 || tutorial.Steps.Count > 30)
        {
            errors.Add($"Tutorial must have between 3 and 30 steps (currently {tutorial.Steps.Count}).");
        }
        else
        {
            var badStepOrders = tutorial.Steps
                .Where(s => string.IsNullOrWhiteSpace(s.Content) || string.IsNullOrWhiteSpace(s.MediaUrl))
                .Select(s => s.StepOrder)
                .ToList();
            if (badStepOrders.Count > 0)
                errors.Add($"Steps {string.Join(", ", badStepOrders)} are missing description or image.");
        }

        if (tutorial.Type == TutorialType.VIP)
        {
            var vipSettings = await _tutorialRepo.GetActiveCreatorVipSettingsAsync(authorId, ct);
            if (vipSettings is null)
                errors.Add("VIP tutorials require an active VIP pricing tier. BR-13.");
        }

        if (errors.Count > 0)
            throw new DomainException(string.Join(" ", errors));

        var fromStatus = tutorial.Status;
        tutorial.Status = TutorialStatus.PendingCTVReview;
        tutorial.UpdatedAt = DateTime.UtcNow;

        await _tutorialRepo.UpdateAsync(tutorial, ct);

        // IMMUTABLE — INSERT only (BR-17)
        await _tutorialRepo.AddReviewHistoryAsync(new TutorialReviewHistory
        {
            Id = Guid.NewGuid(),
            TutorialId = tutorial.Id,
            ReviewerId = authorId,
            ReviewerRole = UserRoleType.User,
            FromStatus = fromStatus,
            ToStatus = TutorialStatus.PendingCTVReview,
            Action = "Submit",
            Reason = null,
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _notifications.NotifyUsersWithRoleAsync(
            UserRoleType.ContributorReviewer,
            NotificationType.NewTutorialPendingReview,
            "A new tutorial is awaiting your contributor review.",
            "Tutorial",
            tutorial.Id,
            ct);

        return MapToResponse(tutorial);
    }

    public async Task<PagedResult<TutorialListItemResponse>> GetMyTutorialsAsync(
        Guid authorId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var result = await _tutorialRepo.GetByAuthorAsync(authorId, page, pageSize, ct);

        var items = result.Items.Select(t => new TutorialListItemResponse(
            t.Id,
            t.Title,
            t.Slug,
            t.CoverImageUrl,
            t.Type.ToString(),
            t.Difficulty,
            t.Status.ToString(),
            t.Steps.Count,
            t.CreatedAt
        )).ToList();

        return new PagedResult<TutorialListItemResponse>(
            items,
            result.TotalCount,
            result.Page,
            result.PageSize,
            result.TotalPages);
    }

    public async Task<PagedResult<TutorialReviewItemResponse>> GetContributorQueueAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var result = await _tutorialRepo.GetPendingContributorReviewAsync(page, pageSize, ct);

        var items = result.Items.Select(t => new TutorialReviewItemResponse(
            t.Id,
            t.Title,
            t.Slug,
            t.Author.Profile?.DisplayName ?? t.Author.Email,
            t.Steps.Count,
            t.CreatedAt
        )).ToList();

        return new PagedResult<TutorialReviewItemResponse>(
            items,
            result.TotalCount,
            result.Page,
            result.PageSize,
            result.TotalPages);
    }

    public async Task ContributorApproveAsync(
        Guid tutorialId, Guid reviewerId, CancellationToken ct = default)
    {
        var tutorial = await _tutorialRepo.GetByIdWithStepsAsync(tutorialId, ct)
            ?? throw new NotFoundException($"Tutorial {tutorialId} not found.");

        if (tutorial.Status != TutorialStatus.PendingCTVReview)
            throw new DomainException("This tutorial is not in the contributor review queue.");

        var fromStatus = tutorial.Status;
        tutorial.Status = TutorialStatus.PendingManagerReview;
        tutorial.UpdatedAt = DateTime.UtcNow;

        await _tutorialRepo.UpdateAsync(tutorial, ct);

        // IMMUTABLE — INSERT only (BR-17)
        await _tutorialRepo.AddReviewHistoryAsync(new TutorialReviewHistory
        {
            Id = Guid.NewGuid(),
            TutorialId = tutorial.Id,
            ReviewerId = reviewerId,
            ReviewerRole = UserRoleType.ContributorReviewer,
            FromStatus = fromStatus,
            ToStatus = TutorialStatus.PendingManagerReview,
            Action = "ContributorApprove",
            Reason = null,
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _notifications.NotifyUsersWithRoleAsync(
            UserRoleType.Manager,
            NotificationType.TutorialReadyForManagerApproval,
            "A tutorial has passed contributor review and awaits your approval.",
            "Tutorial",
            tutorial.Id,
            ct);
    }

    public async Task ContributorRequestRevisionAsync(
        Guid tutorialId, Guid reviewerId, string reason, CancellationToken ct = default)
    {
        if (reason.Length < 10)
            throw new DomainException("Revision reason must be at least 10 characters. BR-18.");

        var tutorial = await _tutorialRepo.GetByIdWithStepsAsync(tutorialId, ct)
            ?? throw new NotFoundException($"Tutorial {tutorialId} not found.");

        if (tutorial.Status != TutorialStatus.PendingCTVReview)
            throw new DomainException("This tutorial is not in the contributor review queue.");

        var fromStatus = tutorial.Status;
        tutorial.Status = TutorialStatus.RevisionRequired;
        tutorial.UpdatedAt = DateTime.UtcNow;

        await _tutorialRepo.UpdateAsync(tutorial, ct);

        // IMMUTABLE — INSERT only (BR-17)
        await _tutorialRepo.AddReviewHistoryAsync(new TutorialReviewHistory
        {
            Id = Guid.NewGuid(),
            TutorialId = tutorial.Id,
            ReviewerId = reviewerId,
            ReviewerRole = UserRoleType.ContributorReviewer,
            FromStatus = fromStatus,
            ToStatus = TutorialStatus.RevisionRequired,
            Action = "RequestRevision",
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _notifications.NotifyUserAsync(
            tutorial.AuthorId,
            NotificationType.TutorialRevisionRequired,
            $"Your tutorial requires revision from a contributor: {reason}",
            "Tutorial",
            tutorial.Id,
            ct);
    }

    private static TutorialResponse MapToResponse(Tutorial tutorial) => new(
        tutorial.Id,
        tutorial.Slug,
        tutorial.Title,
        tutorial.Description,
        tutorial.CoverImageUrl,
        tutorial.Type.ToString(),
        tutorial.Difficulty,
        tutorial.CategoryId,
        tutorial.Status.ToString(),
        tutorial.CreatedAt,
        tutorial.UpdatedAt);

    private async Task<string> GenerateUniqueSlugAsync(string title, CancellationToken ct)
    {
        var baseSlug = Regex.Replace(title.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        if (baseSlug.Length > 110)
            baseSlug = baseSlug[..110].TrimEnd('-');

        var slug = baseSlug;
        var suffix = 2;
        while (await _tutorialRepo.SlugExistsAsync(slug, ct))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }
        return slug;
    }
}
