using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public AccountStatus Status { get; set; }
    public string? VerificationToken { get; set; }
    public DateTime? TokenExpiry { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public UserProfile? Profile { get; set; }
    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
    public ICollection<Tutorial> Tutorials { get; set; } = new List<Tutorial>();
    public ICollection<VipSubscription> VipSubscriptions { get; set; } = new List<VipSubscription>();
    public ICollection<Achievement> Achievements { get; set; } = new List<Achievement>();
    public ICollection<FollowRelationship> Following { get; set; } = new List<FollowRelationship>();
    public ICollection<FollowRelationship> Followers { get; set; } = new List<FollowRelationship>();
}
