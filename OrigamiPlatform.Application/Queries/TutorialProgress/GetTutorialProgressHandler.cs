using OrigamiPlatform.Application.DTOs.TutorialProgress;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.TutorialProgress;

public class GetTutorialProgressHandler
{
    private readonly ITutorialStepProgressRepository _progress;

    public GetTutorialProgressHandler(ITutorialStepProgressRepository progress)
        => _progress = progress;

    public async Task<TutorialProgressDto> HandleAsync(
        GetTutorialProgressQuery query, CancellationToken ct = default)
    {
        if (!await _progress.IsPublishedTutorialAsync(query.TutorialId, ct))
            throw new NotFoundException("Published tutorial not found.");

        var total = await _progress.CountStepsAsync(query.TutorialId, ct);
        var completedIds = await _progress.GetCompletedStepIdsAsync(query.UserId, query.TutorialId, ct);
        return TutorialProgressFactory.Create(query.TutorialId, total, completedIds);
    }
}
