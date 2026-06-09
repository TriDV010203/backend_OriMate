namespace OrigamiPlatform.Domain.Entities;

public class Journal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? LinkedTutorialId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrls { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Tutorial? LinkedTutorial { get; set; }
}
