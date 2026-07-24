namespace OrigamiPlatform.Application.Queries.Tutorials;

/// <summary>Fetches any tutorial by id regardless of author, for the admin edit form. Authorization
/// (Admin/Manager role) is enforced at the controller.</summary>
public record GetTutorialForAdminQuery(Guid TutorialId);
