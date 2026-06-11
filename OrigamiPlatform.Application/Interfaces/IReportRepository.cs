using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Interfaces
{
    public interface IReportRepository
    {
        Task<bool> HasUserReportedItemAsync(Guid userId, Guid targetId, TargetType targetType);
        Task<Report> AddAsync(Report report);
        Task<Report?> GetByIdAsync(Guid id);
        Task UpdateAsync(Report report);
        Task<List<Report>> GetPendingReportsAsync(int skip, int take);
    }
}