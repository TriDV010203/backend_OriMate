using Microsoft.Extensions.DependencyInjection;
using OrigamiPlatform.Application.Commands.Achievements;
using OrigamiPlatform.Application.Commands.Auth;
using OrigamiPlatform.Application.Commands.Journals;

using OrigamiPlatform.Application.Features.Tutorials.Services;

using OrigamiPlatform.Application.Commands.CommunityPosts;
using OrigamiPlatform.Application.Commands.FamilyProjects;
using OrigamiPlatform.Application.Commands.Likes;
using OrigamiPlatform.Application.Commands.Reports;

using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Application.Queries.CommunityPosts;
using OrigamiPlatform.Application.Queries.Reports;
using OrigamiPlatform.Application.Queries.Achievements;
using OrigamiPlatform.Application.Queries.FamilyProjects;
using OrigamiPlatform.Application.Queries.Journals;
using OrigamiPlatform.Application.Queries.Tutorials;
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
        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<IJournalRepository, JournalRepository>();

        // FT-18 Family plan & projects
        services.AddScoped<IFamilySubscriptionRepository, FamilySubscriptionRepository>();
        services.AddScoped<IFamilyProjectRepository, FamilyProjectRepository>();

        // FT-09-Repositories
        services.AddScoped<ICommunityPostRepository, CommunityPostRepository>();
        services.AddScoped<ILikeRepository, LikeRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IBlockedWordRepository, BlockedWordRepository>();

        // Services
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IBlockedWordService, BlockedWordService>();

        // Handlers — Auth
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<VerifyEmailHandler>();
        services.AddScoped<ResendVerificationHandler>();
        services.AddScoped<ForgotPasswordHandler>();
        services.AddScoped<ResetPasswordHandler>();

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

        // Handlers — FT-18 Family projects
        services.AddScoped<CreateFamilyProjectHandler>();
        services.AddScoped<GetFamilyProjectHandler>();


        return services;
    }
}
