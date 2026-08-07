using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.Reports;

public record PendingReportDto(
    Guid Id,
    Guid ReporterId,
    TargetType TargetType,
    Guid TargetId,
    string Reason,
    DateTime CreatedAt,
    string? TargetContent,
    // Slug của Tutorial để FE dựng link xem chi tiết (TargetType=Tutorial, hoặc RootTargetType=Tutorial khi báo cáo là 1 Comment)
    string? TargetSlug = null,
    // Khi TargetType=Comment: loại nội dung gốc chứa comment này sau khi đã lần theo chuỗi reply (CommunityPost/Tutorial/StuckThread)
    TargetType? RootTargetType = null,
    Guid? RootTargetId = null
);