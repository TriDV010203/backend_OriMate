using Microsoft.Extensions.DependencyInjection;
using OrigamiPlatform.Application.Commands.Achievements;
using OrigamiPlatform.Application.Commands.Auth;
using OrigamiPlatform.Application.Commands.Comments;
using OrigamiPlatform.Application.Commands.CommunityPosts;
using OrigamiPlatform.Application.Commands.Follows;
using OrigamiPlatform.Application.Commands.Journals;
using OrigamiPlatform.Application.Commands.Likes;
using OrigamiPlatform.Application.Commands.Notifications;
using OrigamiPlatform.Application.Commands.Reports;
using OrigamiPlatform.Application.Commands.TutorialProgress;
using OrigamiPlatform.Application.Commands.Users;
using OrigamiPlatform.Application.Commands.Wishlists;
using OrigamiPlatform.Application.Features.AdminConfiguration.Services;
using OrigamiPlatform.Application.Features.Tutorials.Services;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Application.Queries.Achievements;
using OrigamiPlatform.Application.Queries.Comments;
using OrigamiPlatform.Application.Queries.CommunityPosts;
using OrigamiPlatform.Application.Queries.Journals;
using OrigamiPlatform.Application.Queries.Notifications;
using OrigamiPlatform.Application.Queries.Reports;
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

        // Services
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IBlockedWordService, BlockedWordService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Admin
        services.AddScoped<IAdminConfigService, AdminConfigService>();

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

        // Services — Tutorials FT-04
        services.AddScoped<ITutorialService, TutorialService>();

        services.AddScoped<CreateCommunityPostHandler>();
        services.AddScoped<ToggleLikeHandler>();
        services.AddScoped<SubmitReportHandler>();
        services.AddScoped<HandleReportHandler>();
        services.AddScoped<GetCommunityFeedHandler>();
        services.AddScoped<GetPendingReportsHandler>();
        services.AddScoped<CreateAchievementHandler>();
        services.AddScoped<UpdateAchievementHandler>();
        services.AddScoped<DeleteAchievementHandler>();
        services.AddScoped<GetUserAchievementsHandler>();
        services.AddScoped<CreateJournalHandler>();
        services.AddScoped<UpdateJournalHandler>();
        services.AddScoped<DeleteJournalHandler>();
        services.AddScoped<GetUserJournalsHandler>();
        services.AddScoped<AddCommentHandler>();
        services.AddScoped<DeleteCommentHandler>();
        services.AddScoped<GetCommentsHandler>();
        services.AddScoped<ToggleWishlistHandler>();
        services.AddScoped<GetWishlistHandler>();
        services.AddScoped<ToggleFollowHandler>();
        services.AddScoped<MarkNotificationAsReadHandler>();
        services.AddScoped<MarkAllNotificationsAsReadHandler>();
        services.AddScoped<GetNotificationsHandler>();
        services.AddScoped<GetCreatorProfileHandler>();
        services.AddScoped<UpdateProfileHandler>();

        // Handlers — Tutorial step progress (per user)
        services.AddScoped<CompleteTutorialStepHandler>();
        services.AddScoped<UncompleteTutorialStepHandler>();
        services.AddScoped<GetTutorialProgressHandler>();

        // Handlers — VIP Subscription (FT-16, FT-17)
        services.AddScoped<ConfigureVipTierHandler>();
        services.AddScoped<SubscribeHandler>();
        services.AddScoped<ConfirmPaymentHandler>();
        services.AddScoped<RejectPaymentHandler>();
        services.AddScoped<GetMySubscriptionsHandler>();
        services.AddScoped<GetCreatorRevenueHandler>();

        return services;
    }
}
