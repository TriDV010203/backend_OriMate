using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class Comment
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public TargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User Author { get; set; } = null!;
}
