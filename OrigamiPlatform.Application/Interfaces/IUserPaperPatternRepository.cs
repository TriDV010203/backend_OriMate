using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IUserPaperPatternRepository
{
    Task<List<UserPaperPattern>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, Guid patternId, CancellationToken ct = default);
    Task AddAsync(UserPaperPattern userPaperPattern, CancellationToken ct = default);
}
