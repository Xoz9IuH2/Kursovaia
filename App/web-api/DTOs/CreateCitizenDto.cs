namespace web_api.DTOs;

public class CreateCitizenDto
{
    public string Login { get; set; } = string.Empty;
    public int PersonalFileId { get; set; }
}