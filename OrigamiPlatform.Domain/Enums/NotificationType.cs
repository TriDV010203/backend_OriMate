namespace OrigamiPlatform.Domain.Enums;

public enum NotificationType
{
    Follow,
    Like,
    Comment,
    TutorialStatusChanged,
    System,

    NewComment,
    NewFollower,
    NewLike,

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
    MilestoneUnlocked,

    // FT-33 Learning Path completion reward
    LearningPathCompleted,

    // Streak milestone reward (7/14/30-day streaks)
    StreakMilestoneReached,

    // FT-34 Daily Challenge
    TutorialSelectedAsChallenge,
    DailyChallengeResult,

    // FT-35 Badge System
    BadgeEarned,

    // Learning Path Mode unlock test review
    NewModeUnlockSubmission,
    ModeUnlockApproved,
    ModeUnlockRejected
}
