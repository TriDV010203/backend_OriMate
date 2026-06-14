using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Journals;

public class DeleteJournalHandler
{
    private readonly IJournalRepository _journals;

    public DeleteJournalHandler(IJournalRepository journals)
        => _journals = journals;

    public async Task HandleAsync(DeleteJournalCommand command, CancellationToken ct = default)
    {
        var journal = await _journals.GetByIdAsync(command.JournalId, ct)
            ?? throw new NotFoundException("Journal not found.");

        if (journal.UserId != command.UserId)
            throw new ForbiddenException("You can only delete your own journal.");

        await _journals.DeleteAsync(journal, ct);
    }
}
