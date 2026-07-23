namespace OrigamiPlatform.Domain.Entities;

public class UserDailyQuestProgress
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid QuestId { get; set; }
    public DateOnly QuestDate { get; set; }
    public int Progress { get; set; }
    public bool IsCompleted { get; set; }

    public User User { get; set; } = null!;
    public DailyQuest Quest { get; set; } = null!;
}
