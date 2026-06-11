using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Interfaces
{
    public interface IReportRepository
    {
        // Kiểm tra xem User đã report item này chưa (Ngăn duplicate report - NAC-02)
        Task<bool> HasUserReportedItemAsync(Guid userId, Guid targetId, TargetType targetType);
        Task<Report> AddAsync(Report report);
        Task<Report?> GetByIdAsync(Guid id);
        Task UpdateAsync(Report report);
    }
}