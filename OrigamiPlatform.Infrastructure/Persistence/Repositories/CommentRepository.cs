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

    public async Task UpdateAsync(Comment comment)
    {
        _context.Comments.Update(comment);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<Comment>> GetCommentsByTargetAsync(Guid targetId, TargetType targetType, int page, int pageSize)
    {
        var query = _context.Comments
            .Where(c => c.TargetId == targetId && c.TargetType == targetType && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Comment>(items, totalCount, page, pageSize, totalPages);
    }

    public async Task<List<Comment>> GetRepliesByParentIdsAsync(IEnumerable<Guid> parentIds)
    {
        return await _context.Comments
            .Where(c => c.TargetType == TargetType.Comment && parentIds.Contains(c.TargetId) && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetCommentCountAsync(Guid targetId, TargetType targetType, CancellationToken ct = default)
    {
        return await _context.Comments
            .CountAsync(c => c.TargetId == targetId && c.TargetType == targetType && !c.IsDeleted, ct);
    }
}