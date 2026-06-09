namespace OrigamiPlatform.Domain.Entities;

public class Comment
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public Guid? PostId { get; set; }
    public Guid? TutorialId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User Author { get; set; } = null!;
    public CommunityPost? Post { get; set; }
    public Tutorial? Tutorial { get; set; }
    public ICollection<Like> Likes { get; set; } = new List<Like>();
}
