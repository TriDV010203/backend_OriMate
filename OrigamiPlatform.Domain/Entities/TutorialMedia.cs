namespace OrigamiPlatform.Domain.Entities;

public class TutorialMedia
{
    public Guid Id { get; set; }
    public Guid TutorialId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tutorial Tutorial { get; set; } = null!;
}
