using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class WishlistRepository : IWishlistRepository
{
    private readonly AppDbContext _context;

    public WishlistRepository(AppDbContext context) => _context = context;

    public async Task<Wishlist?> GetByUserAndTargetAsync(Guid userId, Guid targetId, TargetType targetType, CancellationToken ct = default)
    {
        return await _context.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.TargetId == targetId && w.TargetType == targetType, ct);
    }

    public async Task AddAsync(Wishlist wishlist, CancellationToken ct = default)
    {
        await _context.Wishlists.AddAsync(wishlist, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(Wishlist wishlist, CancellationToken ct = default)
    {
        _context.Wishlists.Remove(wishlist);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<Wishlist>> GetUserWishlistAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Wishlists.Where(w => w.UserId == userId);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(w => w.CreatedAt) // Bài lưu gần nhất xếp lên đầu
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<Wishlist>(items, totalCount, page, pageSize, totalPages);
    }

    public async Task<int> GetWishlistCountAsync(Guid targetId, TargetType targetType, CancellationToken ct = default)
    {
        return await _context.Wishlists
            .CountAsync(w => w.TargetId == targetId && w.TargetType == targetType, ct);
    }

    // Nhớ thêm TargetType? targetType vào chữ ký hàm (và phải khớp với IWishlistRepository của bạn)
    public async Task<PagedResult<Wishlist>> GetUserWishlistAsync(Guid userId, TargetType? targetType, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Wishlists.Where(w => w.UserId == userId);

        // THÊM ĐOẠN NÀY ĐỂ LỌC THEO TAB
        if (targetType.HasValue)
        {
            query = query.Where(w => w.TargetType == targetType.Value);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<Wishlist>(items, totalCount, page, pageSize, totalPages);
    }
}