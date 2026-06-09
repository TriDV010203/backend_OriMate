namespace OrigamiPlatform.Domain.Entities;

public class CommunityPost
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public Guid? LinkedTutorialId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsVisible { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User Author { get; set; } = null!;
    public Tutorial? LinkedTutorial { get; set; }
    public ICollection<CommunityPostMedia> Media { get; set; } = new List<CommunityPostMedia>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
