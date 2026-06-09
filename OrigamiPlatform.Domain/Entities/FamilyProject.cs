using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class FamilyProject
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public Guid TutorialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User Creator { get; set; } = null!;
    public Tutorial Tutorial { get; set; } = null!;
    public ICollection<FamilyProjectMember> Members { get; set; } = new List<FamilyProjectMember>();
    public ICollection<FamilyProjectStepProgress> StepProgresses { get; set; } = new List<FamilyProjectStepProgress>();
}
