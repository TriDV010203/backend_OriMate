namespace OrigamiPlatform.Domain.Entities;

public class FollowRelationship
{
    public Guid Id { get; set; }
    public Guid FollowerId { get; set; }
    public Guid FollowedId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User Follower { get; set; } = null!;
    public User Followed { get; set; } = null!;
}
