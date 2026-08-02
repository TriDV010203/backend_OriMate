using OrigamiPlatform.Application.DTOs.Gamification;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Gamification;

public class GetMyQuestProgressHandler
{
    private readonly IDailyQuestRepository _dailyQuests;
    private readonly IUserDailyQuestProgressRepository _questProgress;

    public GetMyQuestProgressHandler(
        IDailyQuestRepository dailyQuests, IUserDailyQuestProgressRepository questProgress)
        => (_dailyQuests, _questProgress) = (dailyQuests, questProgress);

    /// <summary>Null if there is no active daily quest.</summary>
    public async Task<QuestProgressDto?> HandleAsync(GetMyQuestProgressQuery query, CancellationToken ct = default)
    {
        var quest = (await _dailyQuests.GetActiveAsync(ct)).FirstOrDefault();
        if (quest is null)
            return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var progress = await _questProgress.GetAsync(query.UserId, quest.Id, today, ct);

        return new QuestProgressDto(quest.Title, progress?.Progress ?? 0, quest.TargetValue, progress?.IsCompleted ?? false);
    }
}
