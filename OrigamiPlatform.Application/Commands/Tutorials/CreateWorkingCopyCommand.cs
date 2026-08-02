namespace OrigamiPlatform.Application.Commands.Tutorials;

public record CreateWorkingCopyCommand(Guid TutorialId, Guid AuthorId);
