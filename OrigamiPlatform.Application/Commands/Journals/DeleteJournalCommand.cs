namespace OrigamiPlatform.Application.Commands.Journals;

public record DeleteJournalCommand(Guid UserId, Guid JournalId);
