using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.Reports;

public record PendingReportDto(
    Guid Id,
    Guid ReporterId,
    TargetType TargetType,
    Guid TargetId,
    string Reason,
    DateTime CreatedAt,
    string? TargetContent
);