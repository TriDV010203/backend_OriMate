using Microsoft.Extensions.DependencyInjection;
using OrigamiPlatform.Application.Commands.Auth;
using OrigamiPlatform.Application.Interfaces;
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

        // Services
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IEmailService, EmailService>();

        // Handlers — Auth
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<VerifyEmailHandler>();
        services.AddScoped<ResendVerificationHandler>();
        services.AddScoped<ForgotPasswordHandler>();
        services.AddScoped<ResetPasswordHandler>();

        // Handlers — Tutorials
        services.AddScoped<GetTutorialsHandler>();
        services.AddScoped<GetTutorialBySlugHandler>();

        return services;
    }
}
