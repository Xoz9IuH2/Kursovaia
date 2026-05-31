namespace web_api.Models;

public class Summon
{
    public int Id { get; set; }
    public int PersonalFileId { get; set; }
    public PersonalFile? PersonalFile { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime SummonDate { get; set; }
    public string Time { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "sent"; // sent, pending, delivered, arrived, completed, no-show
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public int CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
