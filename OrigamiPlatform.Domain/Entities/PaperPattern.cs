namespace OrigamiPlatform.Domain.Entities;

// FT-28: cosmetic paper-pattern catalog, purchasable with Hạt Gấp.
public class PaperPattern
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int PriceInHatGap { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
