using OrigamiPlatform.Application.DTOs.TutorialProgress;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Constants;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.TutorialProgress;

public class CompleteTutorialStepHandler
{
    // BR-SKILL-01: points awarded per tutorial completed, by difficulty
    private static readonly Dictionary<TutorialDifficulty, int> SkillPointsByDifficulty = new()
    {
        [TutorialDifficulty.Beginner] = 1,
        [TutorialDifficulty.Intermediate] = 2,
        [TutorialDifficulty.Advanced] = 3
    };

    private readonly ITutorialStepProgressRepository _progress;
    private readonly IUserRepository _users;

    public CompleteTutorialStepHandler(ITutorialStepProgressRepository progress, IUserRepository users)
        => (_progress, _users) = (progress, users);

    public async Task<TutorialProgressDto> HandleAsync(
        CompleteTutorialStepCommand command, CancellationToken ct = default)
    {
        var step = await _progress.GetStepWithTutorialAsync(command.StepId, ct)
            ?? throw new NotFoundException("Tutorial step not found.");

        if (step.TutorialId != command.TutorialId)
            throw new NotFoundException("This step does not belong to the given tutorial.");

        if (step.Tutorial.Status != TutorialStatus.Published || step.Tutorial.IsDeleted)
            throw new DomainException("You can only track progress on a published tutorial.");

        // Each user can complete a step only once.
        if (await _progress.ExistsAsync(command.UserId, command.StepId, ct))
            throw new DomainException("You have already completed this step.");

        await _progress.AddAsync(new TutorialStepProgress
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            TutorialId = step.TutorialId,
            TutorialStepId = command.StepId,
            CompletedAt = DateTime.UtcNow
        }, ct);

        var total = await _progress.CountStepsAsync(command.TutorialId, ct);
        var completedIds = await _progress.GetCompletedStepIdsAsync(command.UserId, command.TutorialId, ct);

        if (total > 0 && completedIds.Count >= total)
            await AwardSkillPointsAsync(command.UserId, step.Tutorial.Difficulty, ct);

        return TutorialProgressFactory.Create(command.TutorialId, total, completedIds);
    }

    // FT-25: tutorial just completed for the first time (steps can only be completed once each,
    // so this fires exactly once per user per tutorial) — award skill points and recompute SkillLevel.
    // Never let a skill-point failure fail the main complete-step flow.
    private async Task AwardSkillPointsAsync(Guid userId, TutorialDifficulty difficulty, CancellationToken ct)
    {
        try
        {
            var user = await _users.GetByIdAsync(userId, ct);
            if (user?.Profile is null)
                return;

            user.Profile.SkillPoints += SkillPointsByDifficulty[difficulty];
            user.Profile.SkillLevel = user.Profile.SkillPoints switch
            {
                >= SkillLevelThresholds.Advanced => SkillLevel.Advanced,
                >= SkillLevelThresholds.Intermediate => SkillLevel.Intermediate,
                _ => SkillLevel.Beginner
            };

            await _users.UpdateAsync(user, ct);
        }
        catch
        {
            // skill point award failure must not affect the main step-completion flow
        }
    }
}
