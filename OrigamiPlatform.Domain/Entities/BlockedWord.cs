namespace OrigamiPlatform.Domain.Entities;

public class BlockedWord
{
    public int Id { get; set; }
    public string Word { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
