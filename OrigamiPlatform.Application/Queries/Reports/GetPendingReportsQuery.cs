namespace OrigamiPlatform.Application.Queries.Reports;

public record GetPendingReportsQuery(int Page = 1, int PageSize = 20);