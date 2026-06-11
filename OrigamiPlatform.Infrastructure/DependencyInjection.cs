using Microsoft.Extensions.DependencyInjection;
using OrigamiPlatform.Application.Commands.Achievements;
using OrigamiPlatform.Application.Commands.Auth;
using OrigamiPlatform.Application.Commands.CommunityPosts;
using OrigamiPlatform.Application.Commands.Likes;
using OrigamiPlatform.Application.Commands.Reports;
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

        // Handlers — Auth
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<VerifyEmailHandler>();
        services.AddScoped<ResendVerificationHandler>();

        // Handlers — Tutorials
        services.AddScoped<GetTutorialsHandler>();
        services.AddScoped<GetTutorialBySlugHandler>();

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

        return services;
    }
}
