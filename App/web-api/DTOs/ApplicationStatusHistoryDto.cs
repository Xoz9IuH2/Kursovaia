namespace web_api.DTOs;

public class ApplicationStatusHistoryDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string? ChangedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}
