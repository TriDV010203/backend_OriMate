using OrigamiPlatform.Application.DTOs.Journals;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Journals;

public class UpdateJournalHandler
{
    private readonly IJournalRepository _journals;
    private readonly IBlockedWordService _blockedWords;

    public UpdateJournalHandler(IJournalRepository journals, IBlockedWordService blockedWords)
        => (_journals, _blockedWords) = (journals, blockedWords);

    public async Task<JournalDto> HandleAsync(
        UpdateJournalCommand command,
        CancellationToken ct = default)
    {
        var (content, imageUrls) = JournalRequestValidator.Validate(
            command.Request.Content,
            command.Request.ImageUrls);

        // FT-17 NAC-01: reject entries containing blocked words before saving.
        if (await _blockedWords.ContainsBlockedWordAsync(content, ct))
            throw new DomainException("Your journal entry contains blocked words and cannot be saved.");

        var journal = await _journals.GetByIdAsync(command.JournalId, ct)
            ?? throw new NotFoundException("Journal not found.");

        if (journal.UserId != command.UserId)
            throw new ForbiddenException("You can only update your own journal.");

        if (command.Request.LinkedTutorialId is Guid linkedTutorialId)
        {
            var tutorialExists = await _journals.PublishedTutorialExistsAsync(linkedTutorialId, ct);
            if (!tutorialExists)
                throw new NotFoundException("Published tutorial not found.");
        }

        journal.LinkedTutorialId = command.Request.LinkedTutorialId;
        journal.Content = content;
        journal.ImageUrls = JournalImageUrls.Serialize(imageUrls);
        journal.IsPublic = command.Request.IsPublic;
        journal.UpdatedAt = DateTime.UtcNow;

        await _journals.UpdateAsync(journal, ct);

        var updated = await _journals.GetByIdAsync(journal.Id, ct)
            ?? throw new NotFoundException("Journal not found after update.");

        return updated.ToDto();
    }
}
