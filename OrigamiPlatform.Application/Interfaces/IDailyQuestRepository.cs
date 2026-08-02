using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IDailyQuestRepository
{
    Task<IReadOnlyList<DailyQuest>> GetActiveAsync(CancellationToken ct = default);
}
