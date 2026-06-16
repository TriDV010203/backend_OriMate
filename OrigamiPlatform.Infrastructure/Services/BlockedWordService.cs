using Microsoft.Extensions.DependencyInjection;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Infrastructure.Services;

public class BlockedWordService : IBlockedWordService
{
    private HashSet<string> _cache = [];
    private readonly IServiceScopeFactory _scopeFactory;

    public BlockedWordService(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public async Task<bool> ContainsBlockedWordAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        if (_cache.Count == 0) await ReloadAsync();

        var lower = text.ToLower();
        return _cache.Any(w => lower.Contains(w));
    }

    public async Task ReloadAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IBlockedWordRepository>();
        var words = await repo.GetAllAsync();
        _cache = new HashSet<string>(words.Select(w => w.Word.ToLower()));
    }
}
