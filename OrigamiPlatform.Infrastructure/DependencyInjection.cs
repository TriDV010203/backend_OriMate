using Microsoft.Extensions.DependencyInjection;
using OrigamiPlatform.Application.Commands.Achievements;
using OrigamiPlatform.Application.Commands.Auth;
<<<<<<< HEAD
using OrigamiPlatform.Application.Features.Tutorials.Services;
=======
using OrigamiPlatform.Application.Commands.CommunityPosts;
using OrigamiPlatform.Application.Commands.Likes;
using OrigamiPlatform.Application.Commands.Reports;
>>>>>>> ec190ae1879136b564e62496137d1a5256c8caf3
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Application.Queries.CommunityPosts;
using OrigamiPlatform.Application.Queries.Reports;
using OrigamiPlatform.Application.Queries.Achievements;
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

<<<<<<< HEAD
        // Services — Tutorials FT-04
        services.AddScoped<ITutorialService, TutorialService>();
=======
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
>>>>>>> ec190ae1879136b564e62496137d1a5256c8caf3

        return services;
    }
}
