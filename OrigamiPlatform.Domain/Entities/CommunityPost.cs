namespace OrigamiPlatform.Domain.Entities;

public class CommunityPost
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User Author { get; set; } = null!;
    public ICollection<CommunityPostMedia> Media { get; set; } = new List<CommunityPostMedia>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
}
