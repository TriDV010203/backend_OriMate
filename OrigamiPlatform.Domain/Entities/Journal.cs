namespace OrigamiPlatform.Domain.Entities;

public class Journal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TutorialId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Tutorial Tutorial { get; set; } = null!;
}
