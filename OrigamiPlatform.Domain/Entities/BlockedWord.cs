namespace OrigamiPlatform.Domain.Entities;

public class BlockedWord
{
    public Guid Id { get; set; }
    public string Word { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
