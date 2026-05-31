namespace web_api.DTOs;

public class AuditLogDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserRole { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? TableName { get; set; }
    public int? RecordId { get; set; }
    public string? Details { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
