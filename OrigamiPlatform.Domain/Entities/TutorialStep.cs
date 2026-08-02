namespace OrigamiPlatform.Domain.Entities;

public class TutorialStep
{
    public Guid Id { get; set; }
    public Guid TutorialId { get; set; }
    public int StepOrder { get; set; }
    public string Description { get; set; } = string.Empty;  // đổi từ Content, bỏ Title
    public string? ImageUrl { get; set; }                     // đổi từ MediaUrl
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Tutorial Tutorial { get; set; } = null!;
}