namespace OrigamiPlatform.Domain.Enums;

public enum NotificationType
{
    Follow,
    Like,
    Comment,
    TutorialStatusChanged,
    System,

    // FT-04 Tutorial review workflow
    NewTutorialPendingReview,
    TutorialReadyForManagerApproval,
    TutorialRevisionRequired,

    // FT-05 Manager final approval
    TutorialPublished,
    TutorialRejected,
    TutorialRemoved,

    // FT-07 Edit-after-publish
    TutorialEditPublished,
    TutorialEditRejected,

    // FT-03 Admin actions
    AccountSuspended,
    AccountActivated,

    // FT-20 Personal Milestone
    MilestoneUnlocked
}
