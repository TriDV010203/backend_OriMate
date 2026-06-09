namespace OrigamiPlatform.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}
