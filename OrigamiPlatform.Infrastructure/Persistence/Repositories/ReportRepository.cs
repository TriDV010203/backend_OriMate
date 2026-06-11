using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly AppDbContext _context;

        public ReportRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasUserReportedItemAsync(Guid userId, Guid targetId, TargetType targetType)
        {
            return await _context.Reports
                .AnyAsync(r => r.ReporterId == userId && r.TargetId == targetId && r.TargetType == targetType);
        }

        public async Task<Report> AddAsync(Report report)
        {
            await _context.Reports.AddAsync(report);
            await _context.SaveChangesAsync();
            return report;
        }

        public async Task<Report?> GetByIdAsync(Guid id)
        {
            return await _context.Reports.FindAsync(id);
        }

        public async Task UpdateAsync(Report report)
        {
            _context.Reports.Update(report);
            await _context.SaveChangesAsync();
        }
    }
}