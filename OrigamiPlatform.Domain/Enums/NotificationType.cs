namespace OrigamiPlatform.Domain.Enums;

public enum NotificationType
{
    Follow,
    Like,
    Comment,
    TutorialStatusChanged,
    FamilyInvite,
    System,

    // FT-04 Tutorial review workflow
    NewTutorialPendingReview,
    TutorialReadyForManagerApproval,
    TutorialRevisionRequired
}
