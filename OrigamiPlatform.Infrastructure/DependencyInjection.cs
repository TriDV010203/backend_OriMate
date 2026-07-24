using Microsoft.Extensions.DependencyInjection;
using OrigamiPlatform.Application.Commands.Achievements;
using OrigamiPlatform.Application.Commands.AdminConfiguration;
using OrigamiPlatform.Application.Commands.Auth;
using OrigamiPlatform.Application.Commands.Clan;
using OrigamiPlatform.Application.Commands.Comments;
using OrigamiPlatform.Application.Commands.CommunityPosts;
using OrigamiPlatform.Application.Commands.Follows;
using OrigamiPlatform.Application.Commands.Gamification;
using OrigamiPlatform.Application.Commands.Journals;
using OrigamiPlatform.Application.Commands.LearningPaths;
using OrigamiPlatform.Application.Commands.Likes;
using OrigamiPlatform.Application.Commands.Moderation;
using OrigamiPlatform.Application.Commands.Notifications;
using OrigamiPlatform.Application.Commands.Reports;
using OrigamiPlatform.Application.Commands.Shop;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Commands.TutorialProgress;
using OrigamiPlatform.Application.Commands.Users;
using OrigamiPlatform.Application.Commands.Wishlists;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Application.Queries.Achievements;
using OrigamiPlatform.Application.Queries.AdminConfiguration;
using OrigamiPlatform.Application.Queries.Clan;
using OrigamiPlatform.Application.Queries.Comments;
using OrigamiPlatform.Application.Queries.CommunityPosts;
using OrigamiPlatform.Application.Queries.Gamification;
using OrigamiPlatform.Application.Queries.Journals;
using OrigamiPlatform.Application.Queries.LearningPaths;
using OrigamiPlatform.Application.Queries.Notifications;
using OrigamiPlatform.Application.Queries.Reports;
using OrigamiPlatform.Application.Queries.Shop;
using OrigamiPlatform.Application.Queries.Subscriptions;
using OrigamiPlatform.Application.Queries.TutorialProgress;
using OrigamiPlatform.Application.Queries.Tutorials;
using OrigamiPlatform.Application.Queries.Users;
using OrigamiPlatform.Application.Queries.Wishlists;
using OrigamiPlatform.Application.Commands.Subscriptions;
using OrigamiPlatform.Infrastructure.Persistence.Repositories;
using OrigamiPlatform.Infrastructure.Services;

namespace OrigamiPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITutorialRepository, TutorialRepository>();
        services.AddScoped<IVipSubscriptionRepository, VipSubscriptionRepository>();
        services.AddScoped<ICreatorVipSettingsRepository, CreatorVipSettingsRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<IJournalRepository, JournalRepository>();

        services.AddScoped<ICommunityPostRepository, CommunityPostRepository>();
        services.AddScoped<ILikeRepository, LikeRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IBlockedWordRepository, BlockedWordRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IFollowRepository, FollowRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ITutorialStepProgressRepository, TutorialStepProgressRepository>();
        services.AddScoped<IStuckThreadRepository, StuckThreadRepository>();
        services.AddScoped<IShopLinkRepository, ShopLinkRepository>();
        services.AddScoped<IPersonalMilestoneRepository, PersonalMilestoneRepository>();
        services.AddScoped<IClanRepository, ClanRepository>();
        services.AddScoped<IClanMemberRepository, ClanMemberRepository>();
        services.AddScoped<IClanInviteRepository, ClanInviteRepository>();
        services.AddScoped<IStreakLogRepository, StreakLogRepository>();
        services.AddScoped<IDailyQuestRepository, DailyQuestRepository>();
        services.AddScoped<IUserDailyQuestProgressRepository, UserDailyQuestProgressRepository>();
        services.AddScoped<IHatGapTransactionRepository, HatGapTransactionRepository>();
        services.AddScoped<ILearningPathRepository, LearningPathRepository>();

        // Services
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IBlockedWordService, BlockedWordService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<HatGapAwardService>();

        // Handlers — Auth
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<VerifyEmailHandler>();
        services.AddScoped<ResendVerificationHandler>();
        services.AddScoped<ForgotPasswordHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<RefreshTokenHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<LogoutHandler>();

        // Handlers — Tutorials (public)
        services.AddScoped<GetTutorialsHandler>();
        services.AddScoped<GetTutorialBySlugHandler>();
        services.AddScoped<GetMyTutorialsHandler>();
        services.AddScoped<GetManagerQueueHandler>();

        // Handlers — Tutorials authoring & review (FT-04, FT-05)
        services.AddScoped<GetTutorialForAuthorHandler>();
        services.AddScoped<CreateTutorialHandler>();
        services.AddScoped<AdminCreateTutorialHandler>();
        services.AddScoped<UpdateTutorialHandler>();
        services.AddScoped<SubmitTutorialHandler>();
        services.AddScoped<ManagerPublishHandler>();
        services.AddScoped<ManagerRejectHandler>();
        services.AddScoped<ManagerRemoveHandler>();

        // Handlers — Tutorials edit-after-publish (FT-07)
        services.AddScoped<CreateWorkingCopyHandler>();
        services.AddScoped<UpdateWorkingCopyHandler>();
        services.AddScoped<SubmitEditHandler>();
        services.AddScoped<ManagerApproveEditHandler>();
        services.AddScoped<ManagerRejectEditHandler>();

        // Handlers — Tutorials admin management (edit/list any tutorial regardless of author/status)
        services.AddScoped<GetAdminTutorialsHandler>();
        services.AddScoped<GetTutorialForAdminHandler>();
        services.AddScoped<AdminUpdateTutorialHandler>();

        // Handlers — AdminConfiguration (FT-03)
        services.AddScoped<GetCategoriesHandler>();
        services.AddScoped<CreateCategoryHandler>();
        services.AddScoped<UpdateCategoryHandler>();
        services.AddScoped<GetBlockedWordsHandler>();
        services.AddScoped<CreateBlockedWordHandler>();
        services.AddScoped<RemoveBlockedWordHandler>();
        services.AddScoped<GetUsersHandler>();
        services.AddScoped<AssignRoleHandler>();
        services.AddScoped<RemoveRoleHandler>();
        services.AddScoped<SuspendUserHandler>();
        services.AddScoped<ActivateUserHandler>();

        services.AddScoped<CreateCommunityPostHandler>();
        services.AddScoped<ToggleLikeHandler>();
        services.AddScoped<SubmitReportHandler>();
        services.AddScoped<HandleReportHandler>();
        services.AddScoped<GetCommunityFeedHandler>();
        services.AddScoped<GetCommunityPostByIdHandler>();
        services.AddScoped<GetPendingReportsHandler>();
        services.AddScoped<CreateAchievementHandler>();
        services.AddScoped<UpdateAchievementHandler>();
        services.AddScoped<DeleteAchievementHandler>();
        services.AddScoped<GetUserAchievementsHandler>();
        services.AddScoped<GetMyMilestonesHandler>();
        services.AddScoped<CreateJournalHandler>();
        services.AddScoped<UpdateJournalHandler>();
        services.AddScoped<DeleteJournalHandler>();
        services.AddScoped<GetUserJournalsHandler>();
        services.AddScoped<AddCommentHandler>();
        services.AddScoped<DeleteCommentHandler>();
        services.AddScoped<GetCommentsHandler>();

        // Handlers — Moderation (FT-14, Contributor Reviewer)
        services.AddScoped<DeleteViolatingCommentHandler>();

        services.AddScoped<ToggleWishlistHandler>();
        services.AddScoped<GetWishlistHandler>();
        services.AddScoped<ToggleFollowHandler>();
        services.AddScoped<MarkNotificationAsReadHandler>();
        services.AddScoped<MarkAllNotificationsAsReadHandler>();
        services.AddScoped<GetNotificationsHandler>();
        services.AddScoped<GetCreatorProfileHandler>();
        services.AddScoped<UpdateProfileHandler>();
        services.AddScoped<GetFollowersHandler>();
        services.AddScoped<GetFollowingHandler>();

        // Handlers — Tutorial step progress (per user)
        services.AddScoped<CompleteTutorialStepHandler>();
        services.AddScoped<UncompleteTutorialStepHandler>();
        services.AddScoped<GetTutorialProgressHandler>();
        services.AddScoped<RaiseStuckFlagHandler>();

        // Handlers — Gamification (FT-25 Skill Level, FT-26 Streak, FT-27 Daily Quest, FT-28 Hạt Gấp)
        services.AddScoped<GetMySkillLevelHandler>();
        services.AddScoped<GetMyStreakHandler>();
        services.AddScoped<GetMyQuestProgressHandler>();
        services.AddScoped<GetMyHatGapBalanceHandler>();
        services.AddScoped<PurchaseStreakFreezeHandler>();

        // Handlers — VIP Subscription (FT-16, FT-17)
        services.AddScoped<ConfigureVipTierHandler>();
        services.AddScoped<SubscribeHandler>();
        services.AddScoped<ConfirmPaymentHandler>();
        services.AddScoped<RejectPaymentHandler>();
        services.AddScoped<GetMySubscriptionsHandler>();
        services.AddScoped<GetCreatorRevenueHandler>();

        // Handlers — Shop (FT-18)
        services.AddScoped<GetShopLinksHandler>();
        services.AddScoped<CreateShopLinkHandler>();
        services.AddScoped<UpdateShopLinkHandler>();

        // Handlers — Learning Path (FT-33)
        services.AddScoped<GetLearningPathsHandler>();
        services.AddScoped<GetLearningPathByIdHandler>();
        services.AddScoped<GetLearningPathForTutorialHandler>();
        services.AddScoped<GetAdminLearningPathsHandler>();
        services.AddScoped<GetLearningPathForAdminHandler>();
        services.AddScoped<CreateLearningPathHandler>();
        services.AddScoped<UpdateLearningPathHandler>();
        services.AddScoped<PublishLearningPathHandler>();
        services.AddScoped<ArchiveLearningPathHandler>();

        // Handlers — Clan (FT-22)
        services.AddScoped<CreateClanHandler>();
        services.AddScoped<InviteMemberHandler>();
        services.AddScoped<AcceptInviteHandler>();
        services.AddScoped<LeaveClanHandler>();
        services.AddScoped<GetMyClanHandler>();
        services.AddScoped<GetPendingInvitesHandler>();

        return services;
    }
}
