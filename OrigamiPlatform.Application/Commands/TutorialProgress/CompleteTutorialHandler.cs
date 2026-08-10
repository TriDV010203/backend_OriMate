using OrigamiPlatform.Application.Commands.Achievements;
using OrigamiPlatform.Application.DTOs.Achievements;
using OrigamiPlatform.Application.DTOs.TutorialProgress;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.TutorialProgress;

public class CompleteTutorialHandler
{
    private readonly ITutorialStepProgressRepository _progress;
    private readonly IAchievementRepository _achievements;
    private readonly CreateAchievementHandler _createAchievement;
    private readonly ITutorialDifficultyRatingRepository _ratings;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteTutorialHandler(
        ITutorialStepProgressRepository progress,
        IAchievementRepository achievements,
        CreateAchievementHandler createAchievement,
        ITutorialDifficultyRatingRepository ratings,
        IUnitOfWork unitOfWork)
        => (_progress, _achievements, _createAchievement, _ratings, _unitOfWork)
            = (progress, achievements, createAchievement, ratings, unitOfWork);

    public async Task<CompleteTutorialResultDto> HandleAsync(
        CompleteTutorialCommand command, CancellationToken ct = default)
    {
        if (!await _progress.IsPublishedTutorialAsync(command.TutorialId, ct))
            throw new NotFoundException("Published tutorial not found.");

        // Deliberately read-only: this endpoint never auto-completes a missing step for the user —
        // every step must already be completed (e.g. via the per-step auto-advance-on-navigate calls).
        var orderedStepIds = await _progress.GetOrderedStepIdsAsync(command.TutorialId, ct);
        if (orderedStepIds.Count == 0)
            throw new DomainException("This tutorial has no steps.");

        var completedIds = (await _progress.GetCompletedStepIdsAsync(command.UserId, command.TutorialId, ct))
            .ToHashSet();
        var missingStepIds = orderedStepIds.Where(id => !completedIds.Contains(id)).ToList();
        if (missingStepIds.Count > 0)
            throw new DomainException(
                "Complete every step before finishing this tutorial. Missing steps: "
                + string.Join(",", missingStepIds));

        var progress = TutorialProgressFactory.Create(command.TutorialId, orderedStepIds.Count, completedIds.ToList());

        // Redo case: an Achievement already exists for this user+tutorial -> behave like a normal replay,
        // but never grant a second Achievement or a second difficulty rating.
        var existingAchievement = await _achievements.GetByUserAndTutorialAsync(command.UserId, command.TutorialId, ct);
        if (existingAchievement is not null)
            return new CompleteTutorialResultDto(progress, existingAchievement.ToDto(), IsNewCompletion: false);

        AchievementDto? achievementDto = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            achievementDto = await _createAchievement.HandleAsync(
                new CreateAchievementCommand(
                    command.UserId,
                    new CreateAchievementRequest(command.TutorialId, command.PhotoUrl, command.Note, command.IsPublic)),
                ct);

            if (command.PerceivedDifficulty.HasValue
                && !await _ratings.ExistsAsync(command.UserId, command.TutorialId, ct))
            {
                await _ratings.AddAsync(new TutorialDifficultyRating
                {
                    Id = Guid.NewGuid(),
                    UserId = command.UserId,
                    TutorialId = command.TutorialId,
                    Rating = command.PerceivedDifficulty.Value,
                    CreatedAt = DateTime.UtcNow
                }, ct);
            }
        }, ct);

        return new CompleteTutorialResultDto(progress, achievementDto!, IsNewCompletion: true);
    }
}
