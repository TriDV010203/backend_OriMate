using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly AppDbContext _context;

    public CommentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Comment comment)
    {
        await _context.Comments.AddAsync(comment);
        await _context.SaveChangesAsync();
    }

    public async Task<Comment?> GetByIdAsync(Guid id)
    {
        return await _context.Comments.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task RemoveAsync(Comment comment)
    {
        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<Comment>> GetCommentsByTargetAsync(Guid targetId, TargetType targetType, int page, int pageSize)
    {
        var query = _context.Comments
            .Where(c => c.TargetId == targetId && c.TargetType == targetType)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync();

        // Tính tổng số trang (TotalPages)
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Thêm totalPages vào cuối
        return new PagedResult<Comment>(items, totalCount, page, pageSize, totalPages);
    }
}