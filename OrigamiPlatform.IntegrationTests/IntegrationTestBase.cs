using Microsoft.Extensions.DependencyInjection;
using OrigamiPlatform.Infrastructure.Persistence;
using Xunit;

namespace OrigamiPlatform.IntegrationTests;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory _factory;
    protected readonly HttpClient _client;
    protected readonly AppDbContext _dbContext;

    public IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        _factory = factory;

        _client = factory.CreateClient();

        var scope = factory.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }
}