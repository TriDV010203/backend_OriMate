namespace OrigamiPlatform.Application.Interfaces;

public interface IBlockedWordService
{
    Task<bool> ContainsBlockedWordAsync(string text, CancellationToken ct = default);
    Task ReloadAsync();
}
