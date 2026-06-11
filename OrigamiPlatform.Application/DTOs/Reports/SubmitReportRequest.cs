using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.Reports;
public record SubmitReportRequest(TargetType TargetType, Guid TargetId, string Reason);