using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

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
        CampaignStatus status, int page, int pageSize, CancellationToken ct = default)
        => PageAsync(_db.AdCampaigns.Where(c => c.Status == status), page, pageSize, ct);

    public async Task AddImpressionAsync(AdImpression impression, CancellationToken ct = default)
    {
        _db.AdImpressions.Add(impression);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddClickAsync(AdClick click, CancellationToken ct = default)
    {
        _db.AdClicks.Add(click);
        await _db.SaveChangesAsync(ct);
    }

    public Task<int> CountImpressionsAsync(Guid campaignId, CancellationToken ct = default)
        => _db.AdImpressions.CountAsync(i => i.CampaignId == campaignId, ct);

    public Task<int> CountClicksAsync(Guid campaignId, CancellationToken ct = default)
        => _db.AdClicks.CountAsync(c => c.CampaignId == campaignId, ct);

    public async Task<IReadOnlyList<AdCampaign>> GetLiveByPlacementAsync(
        int placementId, DateTime today, CancellationToken ct = default)
        => await _db.AdCampaigns
            .Where(c => c.PlacementId == placementId
                && (c.Status == CampaignStatus.Approved || c.Status == CampaignStatus.Active)
                && c.BudgetRemaining > 0
                && c.StartDate <= today
                && c.EndDate >= today)
            .Include(c => c.Banners)
            .ToListAsync(ct);

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
