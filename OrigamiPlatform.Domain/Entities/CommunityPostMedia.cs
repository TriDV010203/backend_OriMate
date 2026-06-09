using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class CommunityPostMedia
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public MediaType MediaType { get; set; }
    public string Url { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    public CommunityPost Post { get; set; } = null!;
}
