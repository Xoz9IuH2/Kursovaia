namespace web_api.Models;

public class Evader
{
    public int Id { get; set; }
    public int PersonalFileId { get; set; }
    public PersonalFile? PersonalFile { get; set; }
    public string ProtocolNumber { get; set; } = string.Empty;
    public DateTime ProtocolDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "active"; // active, closed, transferred
    public string? Resolution { get; set; }
    public int CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
