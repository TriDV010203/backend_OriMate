namespace OrigamiPlatform.Domain.Entities;

public class EmailLog
{
    public Guid Id { get; set; }
    public Guid? RecipientId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? Recipient { get; set; }
}
