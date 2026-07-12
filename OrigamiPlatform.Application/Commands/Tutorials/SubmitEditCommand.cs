namespace OrigamiPlatform.Application.Commands.Tutorials;

public record SubmitEditCommand(Guid WorkingCopyId, Guid AuthorId);
