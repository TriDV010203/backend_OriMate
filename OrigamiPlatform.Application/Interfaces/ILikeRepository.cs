using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Interfaces
{
    public interface ILikeRepository
    {
        // Kiểm tra xem User đã like mục này chưa
        Task<Like?> GetLikeAsync(Guid userId, Guid targetId, TargetType targetType);
        Task AddAsync(Like like);
        Task RemoveAsync(Like like);
    }
}