namespace OrigamiPlatform.Application.Common;

/// <summary>Shared constants for the tutorial authoring flow.</summary>
public static class TutorialConstants
{
    /// <summary>
    /// Fixed public author name shown for tutorials with Tutorial.IsOfficial = true
    /// (written directly by Admin/Manager staff), regardless of which staff account authored them.
    /// </summary>
    public const string OfficialAuthorDisplayName = "Đội ngũ OriGami";

    /// <summary>
    /// BR-29: number of leading steps of a VIP tutorial visible to non-subscribers before the rest is locked.
    /// </summary>
    public const int VipFreePreviewStepCount = 2;
}
