namespace web_api.DTOs;

public class EvaderDto
{
    public int Id { get; set; }
    public int PersonalFileId { get; set; }
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string ProtocolNumber { get; set; } = string.Empty;
    public DateTime ProtocolDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Resolution { get; set; }
    public DateTime CreatedAt { get; set; }
}
