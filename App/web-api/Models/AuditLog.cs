namespace web_api.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? TableName { get; set; }
    public int? RecordId { get; set; }
    public string? Details { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
