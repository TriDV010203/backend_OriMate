using OrigamiPlatform.Application.DTOs.Journals;

namespace OrigamiPlatform.Application.Commands.Journals;

public record CreateJournalCommand(Guid UserId, CreateJournalRequest Request);
