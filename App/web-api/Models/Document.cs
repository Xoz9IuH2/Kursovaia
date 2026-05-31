namespace web_api.Models;

public class Document
{
    public int Id { get; set; }
    public int PersonalFileId { get; set; }
    public PersonalFile? PersonalFile { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // pdf, image, doc
    public long FileSize { get; set; }
    public string DocumentType { get; set; } = string.Empty; // passport, education, medical, other
    public string Status { get; set; } = "pending"; // pending, approved, rejected
    public string? RejectionReason { get; set; }
    public int UploadedById { get; set; }
    public User? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
