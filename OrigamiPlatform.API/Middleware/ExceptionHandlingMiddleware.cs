using System.Text.Json;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        => (_next, _logger) = (next, logger);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            ConflictException => (409, ex.Message),
            NotFoundException => (404, ex.Message),
            ForbiddenException => (403, ex.Message),
            DomainException => (400, ex.Message),
            _ => (500, "An unexpected error occurred.")
        };

        if (statusCode == 500)
            _logger.LogError(ex, "Unhandled exception");

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(new { error = message },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
