namespace OrigamiPlatform.Domain.Constants;

/// <summary>Fixed system-owned user accounts, seeded once and referenced by Id (never by the acting caller's own Id).</summary>
public static class SystemUsers
{
    /// <summary>
    /// The single fixed author for every tutorial created via the Admin/Manager authoring shortcut,
    /// regardless of which Admin/Manager staff account actually wrote it. Keeps all official tutorials
    /// under one consistent author so downstream features (grouping, transfer, "official content" lookups)
    /// don't have to special-case dozens of individual staff accounts.
    /// </summary>
    public static readonly Guid OfficialTutorialAuthorId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
}
