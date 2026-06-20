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
}
