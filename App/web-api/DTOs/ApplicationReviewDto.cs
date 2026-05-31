namespace web_api.DTOs;

public class ApplicationReviewDto
{
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public string? Comment { get; set; }
}
