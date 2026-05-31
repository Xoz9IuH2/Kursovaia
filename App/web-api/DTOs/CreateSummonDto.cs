namespace web_api.DTOs;

public class CreateSummonDto
{
    public int PersonalFileId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime SummonDate { get; set; }
    public string Time { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
