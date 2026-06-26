using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class AdCampaignRepository : IAdCampaignRepository
{
    private readonly AppDbContext _db;

    public AdCampaignRepository(AppDbContext db) => _db = db;

    public Task<AdPlacement?> GetActivePlacementAsync(int placementId, CancellationToken ct = default)
        => _db.AdPlacements.FirstOrDefaultAsync(p => p.Id == placementId && p.IsActive, ct);

    public async Task AddAsync(AdCampaign campaign, CancellationToken ct = default)
    {
        _db.AdCampaigns.Add(campaign);
        await _db.SaveChangesAsync(ct);
    }

    public Task<AdCampaign?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.AdCampaigns
            .Include(c => c.Placement)
            .Include(c => c.Banners)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task UpdateAsync(AdCampaign campaign, CancellationToken ct = default)
    {
        _db.AdCampaigns.Update(campaign);
        await _db.SaveChangesAsync(ct);
    }

    public Task<(IEnumerable<AdCampaign> Items, int TotalCount)> GetByPartnerAsync(
        Guid partnerId, int page, int pageSize, CancellationToken ct = default)
        => PageAsync(_db.AdCampaigns.Where(c => c.PartnerId == partnerId), page, pageSize, ct);

    public Task<(IEnumerable<AdCampaign> Items, int TotalCount)> GetByStatusAsync(
        Domain.Enums.CampaignStatus status, int page, int pageSize, CancellationToken ct = default)
        => PageAsync(_db.AdCampaigns.Where(c => c.Status == status), page, pageSize, ct);

    private static async Task<(IEnumerable<AdCampaign>, int)> PageAsync(
        IQueryable<AdCampaign> baseQuery, int page, int pageSize, CancellationToken ct)
    {
        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .Include(c => c.Placement)
            .Include(c => c.Banners)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }
}
