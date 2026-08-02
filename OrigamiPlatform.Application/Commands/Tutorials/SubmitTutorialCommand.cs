namespace OrigamiPlatform.Application.Commands.Tutorials;

public record SubmitTutorialCommand(Guid TutorialId, Guid AuthorId);
