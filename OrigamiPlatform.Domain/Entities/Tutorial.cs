using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class Tutorial
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public int CategoryId { get; set; }
    public Guid? ParentTutorialId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public TutorialType Type { get; set; }
    public TutorialStatus Status { get; set; }
    public TutorialDifficulty Difficulty { get; set; }
    public bool IsDeleted { get; set; }

    // Set true only by AdminCreateTutorialHandler: tutorials authored directly by Admin/Manager staff
    // display a fixed author name instead of the individual staff member's, since these are later
    // curated together into learning paths and shouldn't be tied to whichever staff account wrote them.
    public bool IsOfficial { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User Author { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Tutorial? ParentTutorial { get; set; }
    public ICollection<Tutorial> ChildTutorials { get; set; } = new List<Tutorial>();
    public ICollection<TutorialStep> Steps { get; set; } = new List<TutorialStep>();
    public ICollection<TutorialReviewHistory> ReviewHistories { get; set; } = new List<TutorialReviewHistory>();
    public ICollection<Achievement> Achievements { get; set; } = new List<Achievement>();
}
