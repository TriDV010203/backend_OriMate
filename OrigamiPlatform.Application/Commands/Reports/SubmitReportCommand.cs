using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Commands.Reports;

public record SubmitReportCommand(Guid ReporterId, TargetType TargetType, Guid TargetId, string Reason);