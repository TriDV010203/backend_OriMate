namespace OrigamiPlatform.Domain.Entities;

// FT-11: links two independent, already-existing Tutorial rows as a parent/variant pair
// (e.g. an easier or harder take on the same fold), used to power "Next Fold" suggestions.
public class TutorialVariant
{
    public Guid Id { get; set; }
    public Guid ParentTutorialId { get; set; }
    public Guid VariantTutorialId { get; set; }
    public int? DifficultyDelta { get; set; } // negative = easier, positive = harder
    public DateTime CreatedAt { get; set; }

    public Tutorial ParentTutorial { get; set; } = null!;
    public Tutorial VariantTutorial { get; set; } = null!;
}
