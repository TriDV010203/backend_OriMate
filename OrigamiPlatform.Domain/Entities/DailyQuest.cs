namespace OrigamiPlatform.Domain.Entities;

public class DailyQuest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TargetValue { get; set; }
    public bool IsActive { get; set; } = true;
}
