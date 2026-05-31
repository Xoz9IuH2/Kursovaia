namespace web_api.DTOs;

public class CreateEvaderDto
{
    public int PersonalFileId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
