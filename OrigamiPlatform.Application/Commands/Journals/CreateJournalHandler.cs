using OrigamiPlatform.Application.DTOs.Journals;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Journals;

public class CreateJournalHandler
{
    private readonly IJournalRepository _journals;

    public CreateJournalHandler(IJournalRepository journals)
        => _journals = journals;

    public async Task<JournalDto> HandleAsync(
        CreateJournalCommand command,
        CancellationToken ct = default)
    {
        var (content, imageUrls) = JournalRequestValidator.Validate(
            command.Request.Content,
            command.Request.ImageUrls);

        if (command.Request.LinkedTutorialId is Guid linkedTutorialId)
        {
            var tutorialExists = await _journals.PublishedTutorialExistsAsync(linkedTutorialId, ct);
            if (!tutorialExists)
                throw new NotFoundException("Published tutorial not found.");
        }

        var now = DateTime.UtcNow;
        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            LinkedTutorialId = command.Request.LinkedTutorialId,
            Content = content,
            ImageUrls = JournalImageUrls.Serialize(imageUrls),
            IsPublic = command.Request.IsPublic,
            CreatedAt = now
        };

        await _journals.AddAsync(journal, ct);

        var created = await _journals.GetByIdAsync(journal.Id, ct)
            ?? throw new NotFoundException("Journal not found after creation.");

        return created.ToDto();
    }
}
