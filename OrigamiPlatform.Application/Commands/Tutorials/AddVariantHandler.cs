using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public class AddVariantHandler
{
    private readonly ITutorialRepository _tutorialRepo;
    private readonly ITutorialVariantRepository _variantRepo;

    public AddVariantHandler(ITutorialRepository tutorialRepo, ITutorialVariantRepository variantRepo)
        => (_tutorialRepo, _variantRepo) = (tutorialRepo, variantRepo);

    public async Task HandleAsync(AddVariantCommand command, CancellationToken ct = default)
    {
        var parent = await _tutorialRepo.GetByIdWithStepsAsync(command.ParentTutorialId, ct)
            ?? throw new NotFoundException($"Tutorial {command.ParentTutorialId} not found.");

        if (parent.AuthorId != command.RequesterId)
            throw new ForbiddenException("You are not the author of this tutorial.");

        if (command.VariantTutorialId == command.ParentTutorialId)
            throw new DomainException("Cannot link tutorial to itself.");

        // Validated up-front so a bad id surfaces as 404, not a raw FK-violation from the insert below.
        var variantTutorial = await _tutorialRepo.GetByIdWithStepsAsync(command.VariantTutorialId, ct)
            ?? throw new NotFoundException($"Tutorial {command.VariantTutorialId} not found.");

        var existing = await _variantRepo.GetByPairAsync(command.ParentTutorialId, command.VariantTutorialId, ct);
        if (existing is not null)
            throw new DomainException("This tutorial pair is already linked as variants.");

        await _variantRepo.AddAsync(new TutorialVariant
        {
            Id = Guid.NewGuid(),
            ParentTutorialId = command.ParentTutorialId,
            VariantTutorialId = command.VariantTutorialId,
            DifficultyDelta = command.DifficultyDelta,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }
}
