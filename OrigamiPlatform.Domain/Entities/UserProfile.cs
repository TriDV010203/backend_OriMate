namespace OrigamiPlatform.Domain.Entities;

public class UserProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
