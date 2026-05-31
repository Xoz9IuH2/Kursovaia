namespace web_api.Models;

public class ApplicationStatusHistory
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public Application? Application { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public int? ChangedById { get; set; }
    public User? ChangedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
