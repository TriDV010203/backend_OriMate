using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.VisualSearch;

public record ObjectSearchResultDto(
    string? Message = null,
    string? DetectedObject = null,
    List<OrigamiSearchResultItemDto>? BeginnerOptions = null,
    List<OrigamiSearchResultItemDto>? IntermediateOptions = null,
    List<OrigamiSearchResultItemDto>? AdvancedOptions = null
);

public record OrigamiSearchResultItemDto(
    Guid TargetId,
    TargetType TargetType,
    string? Title,
    string? ImageUrl,
    TutorialDifficulty Difficulty
);