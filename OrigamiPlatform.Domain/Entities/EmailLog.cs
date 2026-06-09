namespace OrigamiPlatform.Domain.Entities;

public class EmailLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}
