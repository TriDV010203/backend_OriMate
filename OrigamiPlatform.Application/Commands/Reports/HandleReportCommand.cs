using OrigamiPlatform.Application.DTOs.Reports;

namespace OrigamiPlatform.Application.Commands.Reports;

public record HandleReportCommand(Guid ReportId, Guid ManagerId, ReportActionType ActionType);