namespace web_api.Models;

public class Application
{
    public int Id { get; set; }
    public int PersonalFileId { get; set; }
    public PersonalFile? PersonalFile { get; set; }
    public string Type { get; set; } = string.Empty; // deferment, complaint, request
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, under_review, approved, rejected
    public string? RejectionReason { get; set; }
    public int? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
