namespace OrigamiPlatform.Domain.Entities;

public class TutorialStep
{
    public Guid Id { get; set; }
    public Guid TutorialId { get; set; }
    public int StepOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Tutorial Tutorial { get; set; } = null!;
    public ICollection<FamilyProjectStepProgress> StepProgresses { get; set; } = new List<FamilyProjectStepProgress>();
}
