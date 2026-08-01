using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public class RemoveVariantHandler
{
    private readonly ITutorialRepository _tutorialRepo;
    private readonly ITutorialVariantRepository _variantRepo;

    public RemoveVariantHandler(ITutorialRepository tutorialRepo, ITutorialVariantRepository variantRepo)
        => (_tutorialRepo, _variantRepo) = (tutorialRepo, variantRepo);

    public async Task HandleAsync(RemoveVariantCommand command, CancellationToken ct = default)
    {
        var variant = await _variantRepo.GetByPairAsync(command.ParentTutorialId, command.VariantTutorialId, ct)
            ?? throw new NotFoundException("Variant link not found.");

        var parent = await _tutorialRepo.GetByIdWithStepsAsync(command.ParentTutorialId, ct)
            ?? throw new NotFoundException($"Tutorial {command.ParentTutorialId} not found.");

        if (parent.AuthorId != command.RequesterId)
            throw new ForbiddenException("You are not the author of this tutorial.");

        await _variantRepo.DeleteAsync(variant, ct);
    }
}
