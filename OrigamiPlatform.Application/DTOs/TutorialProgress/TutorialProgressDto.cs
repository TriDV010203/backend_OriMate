namespace OrigamiPlatform.Application.DTOs.TutorialProgress;

public record TutorialProgressDto(
    Guid TutorialId,
    int TotalSteps,
    int CompletedSteps,
    double PercentComplete,
    bool IsCompleted,
    IReadOnlyList<Guid> CompletedStepIds);

public static class TutorialProgressFactory
{
    public static TutorialProgressDto Create(Guid tutorialId, int totalSteps, IReadOnlyList<Guid> completedStepIds)
    {
        var completed = completedStepIds.Count;
        var percent = totalSteps == 0 ? 0d : Math.Round((double)completed / totalSteps * 100, 1);
        var isCompleted = totalSteps > 0 && completed >= totalSteps;
        return new TutorialProgressDto(tutorialId, totalSteps, completed, percent, isCompleted, completedStepIds);
    }
}
