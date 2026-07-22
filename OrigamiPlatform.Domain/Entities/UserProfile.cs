using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class UserProfile
{
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public int SkillPoints { get; set; }
    public SkillLevel SkillLevel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    //// loc them
    //public string? Address { get; set; }
    //public string? PhoneNumber { get; set; }
    //public string? EmailContact { get; set; }
    //public string? FacebookUrl { get; set; }

    public User User { get; set; } = null!;
}
