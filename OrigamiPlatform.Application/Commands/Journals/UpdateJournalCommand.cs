using OrigamiPlatform.Application.DTOs.Journals;

namespace OrigamiPlatform.Application.Commands.Journals;

public record UpdateJournalCommand(
    Guid UserId,
    Guid JournalId,
    UpdateJournalRequest Request);
