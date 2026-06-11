namespace OrigamiPlatform.Application.DTOs.Reports;

public enum ReportActionType
{
    Dismiss = 1,
    RemoveContent = 2,
    SuspendAccount = 3
}

public record HandleReportRequest(ReportActionType ActionType);