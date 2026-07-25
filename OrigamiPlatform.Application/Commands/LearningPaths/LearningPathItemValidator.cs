using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.LearningPaths;

/// <summary>
/// A learning path may only curate tutorials Admin/Manager already authored and published
/// themselves (Tutorial.IsOfficial = true, Status = Published) — shared by Create and Update
/// so both enforce the same rule the same way.
/// </summary>
public static class LearningPathItemValidator
{
    private const int MaxItems = 50;

    public static async Task<List<LearningPathItem>> BuildItemsAsync(
        ITutorialRepository tutorialRepo, List<Guid> tutorialIds, CancellationToken ct)
    {
        tutorialIds ??= new List<Guid>();

        if (tutorialIds.Count > MaxItems)
            throw new DomainException($"A learning path can have at most {MaxItems} tutorials.");

        if (tutorialIds.Distinct().Count() != tutorialIds.Count)
            throw new DomainException("A tutorial cannot appear more than once in the same learning path.");

        if (tutorialIds.Count == 0)
            return new List<LearningPathItem>();

        var tutorials = await tutorialRepo.GetByIdsAsync(tutorialIds, ct);
        var foundIds = tutorials.Select(t => t.Id).ToHashSet();

        var missing = tutorialIds.Where(id => !foundIds.Contains(id)).ToList();
        if (missing.Count > 0)
            throw new NotFoundException($"Tutorial(s) not found: {string.Join(", ", missing)}.");

        var ineligible = tutorials
            .Where(t => !t.IsOfficial || t.Status != TutorialStatus.Published || t.IsDeleted)
            .Select(t => t.Title)
            .ToList();
        if (ineligible.Count > 0)
            throw new DomainException(
                $"Only official, published tutorials can be added to a learning path. Not eligible: {string.Join(", ", ineligible)}.");

        var now = DateTime.UtcNow;
        var tutorialsById = tutorials.ToDictionary(t => t.Id);

        return tutorialIds.Select((id, index) => new LearningPathItem
        {
            Id = Guid.NewGuid(),
            TutorialId = id,
            Tutorial = tutorialsById[id],
            ItemOrder = index + 1,
            CreatedAt = now
        }).ToList();
    }
}
