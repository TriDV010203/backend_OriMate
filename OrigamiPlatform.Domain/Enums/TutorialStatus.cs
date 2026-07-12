namespace OrigamiPlatform.Domain.Enums;

public enum TutorialStatus
{
    Draft,
    PendingManagerReview,
    RevisionRequired,
    Published,
    Removed,
    EditPendingReview,

    // BR-17: terminal status for a working-copy row after ManagerApproveEdit merges it into the original — never used for the original Tutorial
    Merged
}
