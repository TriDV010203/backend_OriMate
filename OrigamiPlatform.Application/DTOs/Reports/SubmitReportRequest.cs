using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.Reports;

// Gửi từ Frontend (Không có ReporterId vì lấy từ Token)
public record SubmitReportRequest(TargetType TargetType, Guid TargetId, string Reason);