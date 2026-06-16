namespace OrigamiPlatform.Application.DTOs.FamilyProjects;

public record FamilyProjectDto(
    Guid Id,
    Guid OwnerId,
    Guid TutorialId,
    string? TutorialTitle,
    string Name,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    IReadOnlyList<FamilyProjectMemberDto> Members);

public record FamilyProjectMemberDto(
    Guid UserId,
    string? Email,
    string Status,
    DateTime InvitedAt,
    DateTime? JoinedAt);
